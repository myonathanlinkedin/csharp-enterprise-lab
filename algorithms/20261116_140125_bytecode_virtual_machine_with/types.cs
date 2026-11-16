using System;

namespace BytecodeVM
{
    // Enumeration of supported opcodes
    public enum Opcode : byte
    {
        // Stack manipulation
        PushConst = 0,   // operand: 64‑bit signed integer
        Pop = 1,
        Dup = 2,
        Swap = 3,

        // Arithmetic (binary, pop two values, push result)
        Add = 10,
        Sub = 11,
        Mul = 12,
        Div = 13,        // integer division, throws on divide‑by‑zero

        // Control flow
        Jump = 20,               // operand: absolute instruction index
        JumpIfZero = 21,         // operand: absolute instruction index (pops value)

        // Termination
        Halt = 255
    }

    // Immutable instruction representation
    public readonly struct Instruction
    {
        public Opcode Op { get; }
        public long Operand { get; }   // Used only by opcodes that need an operand

        public Instruction(Opcode op, long operand = 0)
        {
            Op = op;
            Operand = operand;
        }

        public override string ToString()
        {
            return Operand != 0 ? $"{Op} {Operand}" : Op.ToString();
        }
    }

    // Custom exception hierarchy for VM errors
    public class VmException : Exception
    {
        public VmException(string message) : base(message) { }
    }

    public sealed class StackUnderflowException : VmException
    {
        public StackUnderflowException() : base("Stack underflow") { }
    }

    public sealed class DivisionByZeroException : VmException
    {
        public DivisionByZeroException() : base("Division by zero") { }
    }

    public sealed class InvalidJumpException : VmException
    {
        public InvalidJumpException() : base("Invalid jump target") { }
    }

    public sealed class HaltException : VmException
    {
        public HaltException() : base("Program halted") { }
    }
}
