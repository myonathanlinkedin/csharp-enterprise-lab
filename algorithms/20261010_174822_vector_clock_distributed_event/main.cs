using System;
using System.Collections.Generic;
using VectorClockLib;

namespace VectorClockTest
{
    internal static class Program
    {
        private static int _passed = 0;
        private static int _failed = 0;

        private static void Assert(bool condition, string message = "")
        {
            if (!condition)
                throw new InvalidOperationException("Assertion failed. " + message);
        }

        private static void RunAllTests()
        {
            var tests = new List<Action>
            {
                TestSingleNodeIncrement,
                TestEquality,
                TestLessThan,
                TestGreaterThan,
                TestConcurrent,
                TestMerge,
                TestSnapshotIsolation,
                TestHashCodeConsistency
            };

            foreach (var test in tests)
            {
                try
                {
                    test();
                    _passed++;
                }
                catch (Exception ex)
                {
                    _failed++;
                    Console.WriteLine($"Test {test.Method.Name} FAILED: {ex.Message}");
                }
            }

            Console.WriteLine($"Tests run: {_passed + _failed}, Passed: {_passed}, Failed: {_failed}");
        }

        // 1. Verify that Tick correctly increments a node's counter.
        private static void TestSingleNodeIncrement()
        {
            var vc = new VectorClock();
            vc.Tick("A");
            vc.Tick("A");
            vc.Tick("A");
            var snap = vc.Snapshot();
            Assert(snap.ContainsKey("A"));
            Assert(snap["A"] == 3, "Counter should be 3 after three ticks.");
        }

        // 2. Two identical clocks compare as Equal.
        private static void TestEquality()
        {
            var vc1 = new VectorClock();
            var vc2 = new VectorClock();
            vc1.Tick("X");
            vc1.Tick("Y");
            vc2.Tick("X");
            vc2.Tick("Y");
            Assert(vc1.Compare(vc2) == VectorClockRelation.Equal);
            Assert(vc1.Equals(vc2));
        }

        // 3. Clock with strictly lower counters is LessThan.
        private static void TestLessThan()
        {
            var a = new VectorClock();
            var b = new VectorClock();
            a.Tick("A"); // 1
            b.Tick("A"); // 1
            b.Tick("A"); // 2
            Assert(a.Compare(b) == VectorClockRelation.LessThan);
        }

        // 4. Clock with strictly higher counters is GreaterThan.
        private static void TestGreaterThan()
        {
            var a = new VectorClock();
            var b = new VectorClock();
            a.Tick("A");
            a.Tick("A");
            b.Tick("A");
            Assert(a.Compare(b) == VectorClockRelation.GreaterThan);
        }

        // 5. Clocks that are incomparable are Concurrent.
        private static void TestConcurrent()
        {
            var c1 = new VectorClock();
            var c2 = new VectorClock();
            c1.Tick("A"); // A:1
            c1.Tick("B"); // B:1
            c2.Tick("A"); // A:1
            c2.Tick("A"); // A:2
            c2.Tick("B"); // B:1
            c2.Tick("B"); // B:2
            // Adjust to make them concurrent:
            c1.Tick("A"); // A:2, B:1
            c2.Tick("B"); // A:2, B:3
            // Now c1: A=2,B=1 ; c2: A=2,B=3 => c1 < c2 on B, equal on A => LessThan, not concurrent.
            // Let's craft proper concurrent case:
            var d1 = new VectorClock();
            var d2 = new VectorClock();
            d1.Tick("A"); // A:1
            d1.Tick("B"); // B:1
            d2.Tick("A"); // A:1
            d2.Tick("C"); // C:1
            Assert(d1.Compare(d2) == VectorClockRelation.Concurrent);
        }

        // 6. Merge takes element‑wise maximum.
        private static void TestMerge()
        {
            var left = new VectorClock();
            var right = new VectorClock();
            left.Tick("A"); // A:1
            left.Tick("B"); // B:1
            right.Tick("A"); // A:1
            right.Tick("A"); // A:2
            right.Tick("C"); // C:1
            left.Merge(right);
            var snap = left.Snapshot();
            Assert(snap["A"] == 2);
            Assert(snap["B"] == 1);
            Assert(snap["C"] == 1);
        }

        // 7. Snapshot returns a copy; mutating the snapshot does not affect the clock.
        private static void TestSnapshotIsolation()
        {
            var vc = new VectorClock();
            vc.Tick("X");
            var snap = vc.Snapshot();
            // Attempt to modify the snapshot (should be allowed because it's a copy).
            var dict = (Dictionary<string, long>)snap;
            dict["X"] = 999;
            // Original clock must remain unchanged.
            var snap2 = vc.Snapshot();
            Assert(snap2["X"] == 1);
        }

        // 8. Hash codes are consistent for equal clocks.
        private static void TestHashCodeConsistency()
        {
            var a = new VectorClock();
            var b = new VectorClock();
            a.Tick("M");
            a.Tick("N");
            b.Tick("N");
            b.Tick("M");
            Assert(a.Equals(b));
            Assert(a.GetHashCode() == b.GetHashCode());
        }

        private static void Main()
        {
            RunAllTests();
        }
    }
}
