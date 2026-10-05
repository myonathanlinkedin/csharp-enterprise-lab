using System;
using System.Collections.Generic;
using RustDeriveDemo;

namespace RustDeriveDemo
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void RunTests()
        {
            // Test 1: Struct with Copy derive should be inlineable.
            var copyStruct = new RustStruct(
                name: "PointCopy",
                fields: new[]
                {
                    new RustField("x", "i32"),
                    new RustField("y", "i32")
                },
                derives: DeriveTrait.Copy | DeriveTrait.Debug
            );
            Assert(DeriveEngine.IsInlineable(copyStruct), "Copy-derived struct should be inlineable.");

            // Test 2: Struct with Clone derive and primitive fields should be inlineable.
            var clonePrimitiveStruct = new RustStruct(
                name: "PointClone",
                fields: new[]
                {
                    new RustField("x", "f64"),
                    new RustField("y", "f64")
                },
                derives: DeriveTrait.Clone
            );
            Assert(DeriveEngine.IsInlineable(clonePrimitiveStruct), "Clone-derived struct with primitive fields should be inlineable.");

            // Test 3: Struct with Clone derive but non‑primitive field should NOT be inlineable.
            var cloneComplexStruct = new RustStruct(
                name: "Wrapper",
                fields: new[]
                {
                    new RustField("data", "String") // Non‑primitive.
                },
                derives: DeriveTrait.Clone
            );
            Assert(!DeriveEngine.IsInlineable(cloneComplexStruct), "Clone-derived struct with non‑primitive field should not be inlineable.");

            // Test 4: Struct without any relevant derives should NOT be inlineable.
            var plainStruct = new RustStruct(
                name: "Plain",
                fields: new[]
                {
                    new RustField("value", "i32")
                }
            );
            Assert(!DeriveEngine.IsInlineable(plainStruct), "Struct without Copy/Clone should not be inlineable.");

            // Edge case: Empty struct with Copy derive.
            var emptyCopy = new RustStruct(
                name: "EmptyCopy",
                fields: Array.Empty<RustField>(),
                derives: DeriveTrait.Copy
            );
            Assert(DeriveEngine.IsInlineable(emptyCopy), "Empty struct with Copy should be inlineable.");

            // Edge case: Null argument handling.
            bool threw = false;
            try
            {
                DeriveEngine.IsInlineable(null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }
            Assert(threw, "IsInlineable should throw on null argument.");
        }

        private static void Demo()
        {
            var demoStruct = new RustStruct(
                name: "Demo",
                fields: new[]
                {
                    new RustField("id", "u64"),
                    new RustField("active", "bool")
                },
                derives: DeriveTrait.Clone | DeriveTrait.Debug
            );

            Console.WriteLine($"Struct '{demoStruct.Name}' inlineable? {DeriveEngine.IsInlineable(demoStruct)}");
        }

        public static void Main()
        {
            RunTests();
            Console.WriteLine("All assertions passed.");
            Demo();
        }
    }
}
