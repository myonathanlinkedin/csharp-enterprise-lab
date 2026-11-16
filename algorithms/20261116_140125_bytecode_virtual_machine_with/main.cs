using System;
using System.Collections.Generic;
using BytecodeVM;

namespace BytecodeVM
{
    internal static class Program
    {
        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new Exception(message);
        }

        private static void RunAllTests()
        {
            TestSimpleArithmetic();
            TestStackManipulation();
            TestConditionalJump();
            TestDivisionByZero();
            TestStackUnderflow();
            Console.WriteLine("All tests passed.");
        }

        // 1. Verify basic arithmetic: (2 + 3) * (4 - 1) = 15
        private static void TestSimpleArithmetic()
        {
            var prog = new List<Instruction>
            {
                new Instruction(Opcode.PushConst, 2),
                new Instruction(Opcode.PushConst, 3),
                new Instruction(Opcode.Add),          // stack: 5
                new Instruction(Opcode.PushConst, 4),
                new Instruction(Opcode.PushConst, 1),
                new Instruction(Opcode.Sub),          // stack: 5,3
                new Instruction(Opcode.Mul),          // stack: 15
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            long result = vm.Execute(prog);
            Assert(result == 15, "Simple arithmetic failed");
        }

        // 2. Verify DUP and SWAP work as expected
        private static void TestStackManipulation()
        {
            var prog = new List<Instruction>
            {
                new Instruction(Opcode.PushConst, 42), // [42]
                new Instruction(Opcode.Dup),          // [42,42]
                new Instruction(Opcode.PushConst, 7), // [42,42,7]
                new Instruction(Opcode.Swap),         // [42,7,42]
                new Instruction(Opcode.Add),          // [42,49]
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            long result = vm.Execute(prog);
            // After execution stack top is 49, second element 42 (ignored)
            Assert(result == 49, "Stack manipulation failed");
        }

        // 3. Verify JumpIfZero conditional branching
        private static void TestConditionalJump()
        {
            // Program computes: if (0) push 100 else push 200; halt
            var prog = new List<Instruction>
            {
                new Instruction(Opcode.PushConst, 0),      // 0 -> condition
                new Instruction(Opcode.JumpIfZero, 5),    // jump to index 5 if zero
                new Instruction(Opcode.PushConst, 100),   // not taken
                new Instruction(Opcode.Halt),
                // target of jump:
                new Instruction(Opcode.PushConst, 200),   // index 5
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            long result = vm.Execute(prog);
            Assert(result == 200, "Conditional jump failed");
        }

        // 4. Verify division by zero throws the correct exception
        private static void TestDivisionByZero()
        {
            var prog = new List<Instruction>
            {
                new Instruction(Opcode.PushConst, 10),
                new Instruction(Opcode.PushConst, 0),
                new Instruction(Opcode.Div),
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            bool caught = false;
            try
            {
                vm.Execute(prog);
            }
            catch (DivisionByZeroException)
            {
                caught = true;
            }
            Assert(caught, "Division by zero not detected");
        }

        // 5. Verify stack underflow detection
        private static void TestStackUnderflow()
        {
            var prog = new List<Instruction>
            {
                new Instruction(Opcode.Pop),   // stack empty
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            bool caught = false;
            try
            {
                vm.Execute(prog);
            }
            catch (StackUnderflowException)
            {
                caught = true;
            }
            Assert(caught, "Stack underflow not detected");
        }

        // Entry point
        private static void Main()
        {
            RunAllTests();

            // Demo: compute factorial of 5 using the VM (iterative)
            // Pseudocode:
            //   push 5          ; n
            //   push 1          ; result
            // loop:
            //   dup             ; duplicate n
            //   push 1
            //   sub             ; n-1
            //   swap
            //   mul             ; result * n
            //   dup
            //   push 1
            //   sub
            //   dup
            //   push 0
            //   jumpifzero end
            //   jump loop
            // end:
            //   pop             ; discard zero
            //   halt
            var factorialProg = new List<Instruction>
            {
                new Instruction(Opcode.PushConst, 5),   // n
                new Instruction(Opcode.PushConst, 1),   // result
                // loop start at index 2
                new Instruction(Opcode.Dup),            // duplicate n
                new Instruction(Opcode.PushConst, 1),
                new Instruction(Opcode.Sub),            // n-1
                new Instruction(Opcode.Swap),           // bring result to top
                new Instruction(Opcode.Mul),            // result * n
                new Instruction(Opcode.Dup),            // duplicate new n (which is n-1)
                new Instruction(Opcode.PushConst, 0),
                new Instruction(Opcode.JumpIfZero, 14), // if n-1 == 0 -> exit
                new Instruction(Opcode.Jump, 2),        // repeat loop
                // exit point (index 14)
                new Instruction(Opcode.Pop),            // discard the zero
                new Instruction(Opcode.Halt)
            };

            var vm = new VirtualMachine();
            long fact5 = vm.Execute(factorialProg);
            Console.WriteLine($"Factorial(5) = {fact5}"); // Expected 120
        }
    }
}
