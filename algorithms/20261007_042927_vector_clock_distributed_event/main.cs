using System;
using System.Collections.Generic;
using VectorClockDemo;

namespace VectorClockDemo
{
    internal static class Tests
    {
        public static void RunAll()
        {
            TestIncrement();
            TestMerge();
            TestCompareEqual();
            TestCompareLessThan();
            TestCompareGreaterThan();
            TestCompareConcurrent();
            Console.WriteLine("All VectorClock tests passed.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"Assertion failed: {message}");
        }

        private static void TestIncrement()
        {
            var vc = new VectorClock(0);
            Assert(vc.Increment() == 1, "First increment should be 1");
            Assert(vc.Increment() == 2, "Second increment should be 2");
            Assert(vc.Timestamp[0] == 2, "Timestamp should reflect two increments");
        }

        private static void TestMerge()
        {
            var a = new VectorClock(1);
            var b = new VectorClock(2);

            a.Increment(); // a: {1:1}
            a.Increment(); // a: {1:2}
            b.Increment(); // b: {2:1}
            b.Increment(); // b: {2:2}
            b.Increment(); // b: {2:3}

            a.Merge(b);
            var ts = a.Timestamp;
            Assert(ts[1] == 2, "Process 1 counter unchanged after merge");
            Assert(ts[2] == 3, "Process 2 counter merged correctly");
        }

        private static void TestCompareEqual()
        {
            var a = new VectorClock(0);
            var b = new VectorClock(0);
            Assert(a.Compare(b) == Relation.Equal, "Two fresh clocks are equal");
            a.Increment();
            b.Increment();
            Assert(a.Compare(b) == Relation.Equal, "Clocks with same increments are equal");
        }

        private static void TestCompareLessThan()
        {
            var a = new VectorClock(0);
            var b = new VectorClock(0);
            a.Increment(); // a: {0:1}
            Assert(a.Compare(b) == Relation.GreaterThan, "a > b after a increments");
            Assert(b.Compare(a) == Relation.LessThan, "b < a after a increments");
        }

        private static void TestCompareGreaterThan()
        {
            var a = new VectorClock(0);
            var b = new VectorClock(0);
            b.Increment(); // b: {0:1}
            Assert(b.Compare(a) == Relation.GreaterThan, "b > a after b increments");
            Assert(a.Compare(b) == Relation.LessThan, "a < b after b increments");
        }

        private static void TestCompareConcurrent()
        {
            var a = new VectorClock(1);
            var b = new VectorClock(2);
            a.Increment(); // a: {1:1}
            b.Increment(); // b: {2:1}
            Assert(a.Compare(b) == Relation.Concurrent, "Clocks from different processes are concurrent");
            // Merge one into the other and compare again
            a.Merge(b);
            Assert(a.Compare(b) == Relation.GreaterThan, "After merge, a dominates b");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            try
            {
                Tests.RunAll();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
