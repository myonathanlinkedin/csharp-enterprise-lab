using System;

namespace HyperLogLogNS
{
    internal static class Test
    {
        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void ApproxEqual(double expected, double actual, double tolerance, string context)
        {
            double diff = Math.Abs(expected - actual);
            Assert(diff <= tolerance,
                $"{context}: expected {expected:F6}, actual {actual:F6}, diff {diff:F6} exceeds tolerance {tolerance}");
        }

        public static void RunAll()
        {
            // Test 0: Empty sketch should estimate 0
            var empty = new HyperLogLog(10);
            ApproxEqual(0.0, empty.Estimate(), 1e-9, "Empty sketch");

            // Test 1: Repeated single element
            var single = new HyperLogLog(10);
            for (int i = 0; i < 1000; i++) single.Add("constant");
            ApproxEqual(1.0, single.Estimate(), 0.2, "Single distinct element");

            // Test 2: Known cardinality (100 000 distinct strings)
            var hll = new HyperLogLog(12); // m = 4096, good accuracy
            int n = 100_000;
            for (int i = 0; i < n; i++) hll.Add(i.ToString());
            double estimate = hll.Estimate();
            double relError = Math.Abs(estimate - n) / n;
            Assert(relError < 0.02, $"Relative error {relError:P} exceeds 2 % for n={n}");

            // Test 3: Merge two sketches with overlapping ranges
            var a = new HyperLogLog(12);
            var b = new HyperLogLog(12);
            for (int i = 0; i < 50_000; i++) a.Add(i.ToString());
            for (int i = 30_000; i < 80_000; i++) b.Add(i.ToString());
            a.Merge(b);
            double merged = a.Estimate();
            int trueCardinality = 80_000; // union size
            double relMerge = Math.Abs(merged - trueCardinality) / trueCardinality;
            Assert(relMerge < 0.025, $"Merge relative error {relMerge:P} exceeds 2.5 %");

            // Test 4: Merging sketches of different precision must throw
            var low = new HyperLogLog(8);
            var high = new HyperLogLog(10);
            bool threw = false;
            try
            {
                low.Merge(high);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "Merging sketches with different precisions should throw");

            Console.WriteLine("All HyperLogLog tests passed.");
        }
    }

    public class Program
    {
        public static void Main()
        {
            Test.RunAll();
        }
    }
}
