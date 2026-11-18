using System;
using AdvancedAlgorithms;

namespace AdvancedAlgorithms
{
    internal static class TestHelper
    {
        public static void Assert(bool condition, string message = "Assertion failed.")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }

    internal static class Program
    {
        private static void RunAllTests()
        {
            // Test 1: Basic construction and full‑range query.
            long[] data1 = { 1, 2, 3, 4, 5 };
            var seg1 = new SegmentTree(data1);
            TestHelper.Assert(seg1.QueryRange(0, 4) == 15, "Full range sum should be 15.");

            // Test 2: Single element query.
            TestHelper.Assert(seg1.QueryRange(2, 2) == 3, "Single element at index 2 should be 3.");

            // Test 3: Range update and subsequent query.
            seg1.UpdateRange(1, 3, 10); // add 10 to indices 1,2,3
            TestHelper.Assert(seg1.QueryRange(0, 4) == 45, "After update, total sum should be 45.");
            TestHelper.Assert(seg1.QueryRange(1, 3) == 39, "Sum of indices 1‑3 should be 39.");

            // Test 4: Overlapping updates.
            seg1.UpdateRange(0, 2, -5);
            TestHelper.Assert(seg1.QueryRange(0, 2) == (1 - 5) + (2 + 10 - 5) + (3 + 10 - 5), "After second update, indices 0‑2 mismatch.");

            // Test 5: Edge case – single‑element array.
            long[] data2 = { 42 };
            var seg2 = new SegmentTree(data2);
            TestHelper.Assert(seg2.QueryRange(0, 0) == 42, "Single element tree query failed.");
            seg2.UpdateRange(0, 0, 8);
            TestHelper.Assert(seg2.QueryRange(0, 0) == 50, "Single element update failed.");

            // Test 6: Invalid range arguments.
            bool caught = false;
            try { seg1.QueryRange(-1, 2); } catch (ArgumentOutOfRangeException) { caught = true; }
            TestHelper.Assert(caught, "Negative left index should throw.");

            caught = false;
            try { seg1.UpdateRange(3, 2, 5); } catch (ArgumentException) { caught = true; }
            TestHelper.Assert(caught, "Left > right should throw.");

            // Test 7: Large random test for internal consistency.
            const int n = 1000;
            var rand = new Random(0);
            long[] large = new long[n];
            for (int i = 0; i < n; i++) large[i] = rand.Next(-1000, 1000);
            var segLarge = new SegmentTree(large);
            // Perform 100 random updates and verify via brute‑force.
            for (int iter = 0; iter < 100; iter++)
            {
                int l = rand.Next(0, n);
                int r = rand.Next(l, n);
                long delta = rand.Next(-500, 500);
                segLarge.UpdateRange(l, r, delta);
                for (int i = l; i <= r; i++) large[i] += delta;
                int ql = rand.Next(0, n);
                int qr = rand.Next(ql, n);
                long expected = 0;
                for (int i = ql; i <= qr; i++) expected += large[i];
                long actual = segLarge.QueryRange(ql, qr);
                TestHelper.Assert(actual == expected, $"Mismatch after random update #{iter}.");
            }

            Console.WriteLine("All tests passed successfully.");
        }

        static void Main()
        {
            RunAllTests();
        }
    }
}
