using System;
using WeightedSampling;

namespace WeightedSamplingDemo
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Assertion failed: " + message);
        }

        private static void TestExactSmallStream()
        {
            // Stream of 5 items, capacity larger than stream => exact result
            var sampler = new WeightedSampler(capacity: 10, seed: 42);
            double[] weights = { 1, 2, 3, 4, 5 };
            double[] values = { 10, 20, 30, 40, 50 };
            double trueSum = 0.0;
            double[] trueMoments = new double[3];
            for (int i = 0; i < weights.Length; i++)
            {
                sampler.Add(weights[i], values[i]);
                trueSum += values[i];
                for (int k = 0; k <= 2; k++)
                {
                    trueMoments[k] += Math.Pow(values[i], k);
                }
            }

            double estSum = sampler.EstimateSum();
            double estMoment1 = sampler.EstimateMoment(1);
            double estMoment2 = sampler.EstimateMoment(2);

            // With capacity >= stream size, estimator should be exact
            Assert(Math.Abs(estSum - trueSum) < 1e-9, "Exact sum mismatch");
            Assert(Math.Abs(estMoment1 - trueMoments[1]) < 1e-9, "Exact first moment mismatch");
            Assert(Math.Abs(estMoment2 - trueMoments[2]) < 1e-9, "Exact second moment mismatch");
        }

        private static void TestUnbiasednessMonteCarlo()
        {
            // Monte‑Carlo test: many repetitions, compare average estimate to true sum
            const int repetitions = 2000;
            const int capacity = 30;
            double[] weights = { 0.5, 1.2, 3.0, 0.8, 2.5 };
            double[] values = { 5, 15, 25, 35, 45 };
            double trueSum = 0.0;
            for (int i = 0; i < weights.Length; i++) trueSum += values[i];

            double avgEst = 0.0;
            for (int r = 0; r < repetitions; r++)
            {
                var sampler = new WeightedSampler(capacity, seed: r);
                for (int i = 0; i < weights.Length; i++) sampler.Add(weights[i], values[i]);
                avgEst += sampler.EstimateSum();
            }
            avgEst /= repetitions;

            double relError = Math.Abs(avgEst - trueSum) / trueSum;
            // Expect relative error to be small (≈ 5% for this tiny stream)
            Assert(relError < 0.07, $"Monte‑Carlo unbiasedness failed, relError={relError}");
        }

        private static void TestZeroWeightItems()
        {
            var sampler = new WeightedSampler(capacity: 5, seed: 123);
            sampler.Add(0.0, 999); // should be ignored
            sampler.Add(1.0, 10);
            sampler.Add(0.0, -5);
            Assert(sampler.TotalWeight == 1.0, "Zero weight handling failed");
            Assert(sampler.SampleSize == 1, "Zero weight sample size failed");
            Assert(Math.Abs(sampler.EstimateSum() - 10.0) < 1e-9, "Zero weight sum estimate failed");
        }

        private static void TestMomentEstimation()
        {
            var sampler = new WeightedSampler(capacity: 8, seed: 777);
            double[] weights = { 1, 1, 1, 1 };
            double[] values = { 2, 3, 5, 7 };
            double trueMoment3 = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                sampler.Add(weights[i], values[i]);
                trueMoment3 += Math.Pow(values[i], 3);
            }
            double estMoment3 = sampler.EstimateMoment(3);
            double relError = Math.Abs(estMoment3 - trueMoment3) / trueMoment3;
            Assert(relError < 0.2, $"Moment estimation error too high: {relError}");
        }

        private static void RunAllTests()
        {
            TestExactSmallStream();
            TestUnbiasednessMonteCarlo();
            TestZeroWeightItems();
            TestMomentEstimation();
            Console.WriteLine("All tests passed.");
        }

        private static void Benchmark()
        {
            const int n = 1_000_000;
            const int capacity = 500;
            var sampler = new WeightedSampler(capacity, seed: 42);
            var rand = new Random(42);
            for (int i = 0; i < n; i++)
            {
                double w = rand.NextDouble() + 0.01; // avoid zero
                double v = rand.NextDouble() * 100.0;
                sampler.Add(w, v);
            }
            double sumEst = sampler.EstimateSum();
            Console.WriteLine($"Benchmark: processed {n} items, sample size {sampler.SampleSize}, estimated sum = {sumEst:F2}");
        }

        static void Main()
        {
            RunAllTests();
            Benchmark();
        }
    }
}
