using System;
using System.Collections.Generic;
using CcfImplementation;

namespace CcfTest
{
    internal static class Program
    {
        private static void Main()
        {
            RunAllTests();
            Console.WriteLine("All CCF tests passed.");
        }

        private static void RunAllTests()
        {
            TestEmptyConstruction();
            TestSingleElement();
            TestBuildFromArray();
            TestPointUpdates();
            TestRangeQueries();
            TestRandomStress();
        }

        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void TestEmptyConstruction()
        {
            var ccf = new Ccf(1);
            Assert(ccf.Size == 1, "Size mismatch");
            Assert(ccf.PrefixSum(0) == 0, "Initial prefix sum not zero");
        }

        private static void TestSingleElement()
        {
            var ccf = new Ccf(3);
            ccf.Add(0, 5);
            ccf.Add(1, -2);
            ccf.Add(2, 7);
            Assert(ccf.Get(0) == 5, "Get(0) incorrect");
            Assert(ccf.Get(1) == -2, "Get(1) incorrect");
            Assert(ccf.Get(2) == 7, "Get(2) incorrect");
            Assert(ccf.PrefixSum(0) == 5, "PrefixSum(0) incorrect");
            Assert(ccf.PrefixSum(1) == 3, "PrefixSum(1) incorrect");
            Assert(ccf.PrefixSum(2) == 10, "PrefixSum(2) incorrect");
        }

        private static void TestBuildFromArray()
        {
            long[] data = { 3, 1, 4, 1, 5, 9, 2 };
            var ccf = Ccf.FromArray(data);
            for (int i = 0; i < data.Length; i++)
                Assert(ccf.Get(i) == data[i], $"FromArray Get({i}) mismatch");
            for (int i = 0; i < data.Length; i++)
            {
                long expected = 0;
                for (int j = 0; j <= i; j++) expected += data[j];
                Assert(ccf.PrefixSum(i) == expected, $"FromArray PrefixSum({i}) mismatch");
            }
        }

        private static void TestPointUpdates()
        {
            var ccf = new Ccf(5);
            // Apply a series of updates and verify after each step.
            ccf.Add(2, 10);
            Assert(ccf.Get(2) == 10, "Update 1 failed");
            ccf.Add(2, -3);
            Assert(ccf.Get(2) == 7, "Update 2 failed");
            ccf.Add(0, 4);
            ccf.Add(4, 6);
            Assert(ccf.RangeSum(0, 4) == 17, "Range sum after multiple updates incorrect");
        }

        private static void TestRangeQueries()
        {
            long[] data = { 2, 7, 1, 8, 2, 8 };
            var ccf = Ccf.FromArray(data);
            Assert(ccf.RangeSum(0, 5) == 28, "Full range sum incorrect");
            Assert(ccf.RangeSum(1, 3) == 16, "Middle range sum incorrect");
            Assert(ccf.RangeSum(2, 2) == 1, "Single element range sum incorrect");
        }

        private static void TestRandomStress()
        {
            const int n = 1000;
            const int ops = 5000;
            var rand = new Random(0xC0FFEE);
            var naive = new long[n];
            var ccf = new Ccf(n);

            for (int k = 0; k < ops; k++)
            {
                int idx = rand.Next(n);
                long delta = rand.Next(-1000, 1001);
                naive[idx] += delta;
                ccf.Add(idx, delta);
            }

            // Verify all prefix sums.
            long running = 0;
            for (int i = 0; i < n; i++)
            {
                running += naive[i];
                Assert(ccf.PrefixSum(i) == running, $"Stress test prefix sum mismatch at {i}");
            }

            // Verify random range queries.
            for (int i = 0; i < 100; i++)
            {
                int l = rand.Next(n);
                int r = rand.Next(l, n);
                long expected = 0;
                for (int j = l; j <= r; j++) expected += naive[j];
                Assert(ccf.RangeSum(l, r) == expected, $"Stress test range sum [{l},{r}] mismatch");
            }
        }
    }
}
