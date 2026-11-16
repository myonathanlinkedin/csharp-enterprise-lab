using System;
using System.Collections.Generic;

namespace BytecodeVM
{
    // Core stack‑based virtual machine
    public sealed class VirtualMachine
    {
        // Private backing field for the evaluation stack
        private readonly List<long> _stack = new List<long>();
        private int _pc; // program counter (index into instruction list)

        // Execute a program and return the top‑most stack value after HALT.
        // Throws VmException on any runtime error.
        public long Execute(IReadOnlyList<Instruction> program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            _pc = 0;
            while (true)
            {
                if (_pc < 0 || _pc >= program.Count)
                    throw new InvalidJumpException();

                Instruction instr = program[_pc];
                _pc++; // advance by default; may be overwritten by jumps

                switch (instr.Op)
                {
                    case Opcode.PushConst:
                        Push(instr.Operand);
                        break;

                    case Opcode.Pop:
                        Pop();
                        break;

                    case Opcode.Dup:
                        {
                            long v = Peek();
                            Push(v);
                        }
                        break;

                    case Opcode.Swap:
                        {
                            long a = Pop();
                            long b = Pop();
                            Push(a);
                            Push(b);
                        }
                        break;

                    case Opcode.Add:
                        BinaryOp((x, y) => x + y);
                        break;

                    case Opcode.Sub:
                        BinaryOp((x, y) => x - y);
                        break;

                    case Opcode.Mul:
                        BinaryOp((x, y) => x * y);
                        break;

                    case Opcode.Div:
                        BinaryOp((x, y) =>
                        {
                            if (y == 0) throw new DivisionByZeroException();
                            return x / y;
                        });
                        break;

                    case Opcode.Jump:
                        JumpAbsolute((int)instr.Operand);
                        break;

                    case Opcode.JumpIfZero:
                        {
                            long condition = Pop();
                            if (condition == 0)
                                JumpAbsolute((int)instr.Operand);
                        }
                        break;

                    case Opcode.Halt:
                        // Return the top of stack (or 0 if empty) as program result
                        return _stack.Count > 0 ? Peek() : 0;

                    default:
                        throw new VmException($"Unsupported opcode {instr.Op}");
                }
            }
        }

        // ----- Stack primitives -----
        private void Push(long value) => _stack.Add(value);

        private long Pop()
        {
            if (_stack.Count == 0) throw new StackUnderflowException();
            int idx = _stack.Count - 1;
            long v = _stack[idx];
            _stack.RemoveAt(idx);
            return v;
        }

        private long Peek()
        {
            if (_stack.Count == 0) throw new StackUnderflowException();
            return _stack[_stack.Count - 1];
        }

        // ----- Helper for binary arithmetic -----
        private void BinaryOp(Func<long, long, long> op)
        {
            long right = Pop();
            long left = Pop();
            long result = op(left, right);
            Push(result);
        }

        // ----- Jump handling -----
        private void JumpAbsolute(int target)
        {
            if (target < 0 || target >= int.MaxValue) // simple sanity check
                throw new InvalidJumpException();
            _pc = target;
        }
    }
}
