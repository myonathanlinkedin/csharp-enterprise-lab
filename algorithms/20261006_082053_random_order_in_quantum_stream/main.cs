using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using QuantumStreaming;

namespace QuantumStreaming
{
    internal static class Program
    {
        private static void Main()
        {
            RunAllTests();
            RunBenchmark();
            Console.WriteLine("All tests passed and benchmark completed.");
        }

        private static void RunAllTests()
        {
            TestAddAndCount();
            TestReplenishAndSample();
            TestSampleUniformity();
            TestRobustLowerBound();
        }

        private static void TestAddAndCount()
        {
            var stream = new RandomOrderStream<int>();
            for (int i = 0; i < 10; i++) stream.Add(i);
            if (stream.Count != 10) throw new Exception("Add/Count test failed.");
        }

        private static void TestReplenishAndSample()
        {
            var stream = new RandomOrderStream<string>();
            var batch = Enumerable.Range(0, 100).Select(i => $"Item{i}");
            stream.Replenish(batch);
            if (stream.Count != 100) throw new Exception("Replenish count mismatch.");

            var sample = stream.Sample(5);
            if (sample.Count != 5) throw new Exception("Sample size mismatch.");
            foreach (var s in sample)
            {
                if (s == null) throw new Exception("Sample contains null.");
            }
        }

        private static void TestSampleUniformity()
        {
            var stream = new RandomOrderStream<int>();
            for (int i = 0; i < 50; i++) stream.Add(i);
            const int trials = 20000;
            var freq = new int[50];
            for (int t = 0; t < trials; t++)
            {
                var s = stream.Sample(1);
                freq[s[0]]++;
            }
            double expected = trials / 50.0;
            double chiSq = freq.Select(c => (c - expected) * (c - expected) / expected).Sum();
            // Degrees of freedom = 49, 95% critical value ≈ 66.34
            if (chiSq > 66.34) throw new Exception("Sample uniformity test failed (chi-square too high).");
        }

        private static void TestRobustLowerBound()
        {
            var stream = new RandomOrderStream<double>();
            for (int i = 0; i < 1000; i++) stream.Add(i * 0.1);
            int observed = 400;
            double confidence = 0.99;
            double lower = stream.ComputeRobustLowerBound(observed, confidence);
            if (lower > observed) throw new Exception("Lower bound exceeds observed count.");
            if (lower < 0) throw new Exception("Lower bound negative.");
        }

        private static void RunBenchmark()
        {
            const int elementCount = 1_000_000;
            var stream = new RandomOrderStream<int>();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < elementCount; i++) stream.Add(i);
            sw.Stop();
            Console.WriteLine($"Add {elementCount:N0} items: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            var sample = stream.Sample(1000);
            sw.Stop();
            Console.WriteLine($"Sample 1,000 items: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            double lower = stream.ComputeRobustLowerBound(500_000, 0.95);
            sw.Stop();
            Console.WriteLine($"Compute lower bound: {sw.ElapsedMilliseconds} ms (value={lower:F2})");
        }
    }
}
