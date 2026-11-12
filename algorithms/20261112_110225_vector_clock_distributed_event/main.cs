using System;
using DistributedSystems;

namespace DistributedSystems
{
    internal static class Program
    {
        // Simple assertion helper that throws on failure.
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void TestIncrement()
        {
            var vc = new VectorClock();
            var vc1 = vc.Increment("A");
            Assert(vc.Get("A") == 0, "Original clock must stay unchanged.");
            Assert(vc1.Get("A") == 1, "Increment should set counter to 1.");
            var vc2 = vc1.Increment("A");
            Assert(vc2.Get("A") == 2, "Second increment should be 2.");
            var vc3 = vc2.Increment("B");
            Assert(vc3.Get("A") == 2 && vc3.Get("B") == 1, "Increment of different node works.");
        }

        private static void TestMerge()
        {
            var vc1 = new VectorClock()
                .Increment("A")   // A:1
                .Increment("A");  // A:2
            var vc2 = new VectorClock()
                .Increment("B")   // B:1
                .Increment("A");  // A:1, B:1

            var merged = vc1.Merge(vc2);
            Assert(merged.Get("A") == 2, "Merge keeps max A.");
            Assert(merged.Get("B") == 1, "Merge keeps max B.");
            // Ensure inputs unchanged
            Assert(vc1.Get("B") == 0 && vc2.Get("A") == 1, "Original clocks unchanged.");
        }

        private static void TestCompare()
        {
            var baseVc = new VectorClock()
                .Increment("A")   // A:1
                .Increment("B");  // B:1

            var descendant = baseVc.Increment("A"); // A:2, B:1
            var ancestor = baseVc;                  // A:1, B:1
            var concurrent = new VectorClock()
                .Increment("A")   // A:1
                .Increment("C");  // C:1

            Assert(ancestor.Compare(descendant) == VectorClock.Relation.Ancestor,
                "Ancestor relationship expected.");
            Assert(descendant.Compare(ancestor) == VectorClock.Relation.Descendant,
                "Descendant relationship expected.");
            Assert(ancestor.Compare(ancestor) == VectorClock.Relation.Equal,
                "Equal relationship expected.");
            Assert(ancestor.Compare(concurrent) == VectorClock.Relation.Concurrent,
                "Concurrent relationship expected.");
        }

        private static void TestEqualityAndHash()
        {
            var vc1 = new VectorClock()
                .Increment("X")
                .Increment("Y");
            var vc2 = new VectorClock()
                .Increment("Y")
                .Increment("X");
            Assert(vc1.Equals(vc2), "Clocks with same entries in different order must be equal.");
            Assert(vc1.GetHashCode() == vc2.GetHashCode(), "Hash codes must match for equal clocks.");
        }

        private static void TestClone()
        {
            var original = new VectorClock()
                .Increment("Node");
            var clone = original.Clone();
            Assert(original.Equals(clone), "Clone must be equal to original.");
            var modified = clone.Increment("Node");
            Assert(!original.Equals(modified), "Modifying clone must not affect original.");
        }

        private static void RunAllTests()
        {
            TestIncrement();
            TestMerge();
            TestCompare();
            TestEqualityAndHash();
            TestClone();
            Console.WriteLine("All VectorClock tests passed.");
        }

        private static void Benchmark()
        {
            const int iterations = 1_000_000;
            var vc = new VectorClock();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                vc = vc.Increment("node");
            }
            sw.Stop();
            Console.WriteLine($"Performed {iterations:N0} increments in {sw.ElapsedMilliseconds} ms.");
        }

        static void Main()
        {
            RunAllTests();
            Benchmark();
        }
    }
}
