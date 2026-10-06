using System;
using PerseusDemo;

class Program
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Assertion failed: " + message);
    }

    static void TestNormalOperation()
    {
        var detector = new PerseusDetector(alpha: 0.2, consecutiveThreshold: 3, sigmaMultiplier: 3.0);
        // Simulate stable latency around 100 ms.
        for (int i = 0; i < 50; i++) detector.AddSample(100 + RandomNoise());

        Assert(!detector.IsFailSlowDetected(), "Normal operation should not trigger detection.");
    }

    static void TestConsecutiveHighLatency()
    {
        var detector = new PerseusDetector(alpha: 0.2, consecutiveThreshold: 3, sigmaMultiplier: 2.5);
        // Warm‑up with normal samples.
        for (int i = 0; i < 30; i++) detector.AddSample(100 + RandomNoise());

        // Insert three high latency samples that exceed the dynamic threshold.
        for (int i = 0; i < 3; i++) detector.AddSample(400 + RandomNoise());

        Assert(detector.IsFailSlowDetected(), "Three consecutive high latencies should trigger detection.");
    }

    static void TestNonConsecutiveHighLatency()
    {
        var detector = new PerseusDetector(alpha: 0.2, consecutiveThreshold: 3, sigmaMultiplier: 2.5);
        for (int i = 0; i < 30; i++) detector.AddSample(100 + RandomNoise());

        // High, low, high, low pattern.
        detector.AddSample(400);
        detector.AddSample(100);
        detector.AddSample(420);
        detector.AddSample(110);
        detector.AddSample(430);

        Assert(!detector.IsFailSlowDetected(), "Non‑consecutive high latencies should not trigger detection.");
    }

    static void TestResetFunctionality()
    {
        var detector = new PerseusDetector(alpha: 0.2, consecutiveThreshold: 2, sigmaMultiplier: 2.0);
        for (int i = 0; i < 20; i++) detector.AddSample(100 + RandomNoise());

        detector.AddSample(500);
        detector.AddSample(520); // detection should fire
        Assert(detector.IsFailSlowDetected(), "Detection expected before reset.");

        detector.Reset();
        Assert(!detector.IsFailSlowDetected(), "Detection should be cleared after reset.");

        // After reset, normal traffic should stay clear.
        for (int i = 0; i < 10; i++) detector.AddSample(100 + RandomNoise());
        Assert(!detector.IsFailSlowDetected(), "Post‑reset normal traffic should not trigger detection.");
    }

    static double RandomNoise()
    {
        // Simple deterministic pseudo‑noise for reproducibility.
        return (new Random()).NextDouble() * 5.0 - 2.5; // ±2.5 ms
    }

    static void Benchmark()
    {
        var detector = new PerseusDetector();
        var rnd = new Random(0);
        const int samples = 1_000_000;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < samples; i++)
        {
            double latency = 100 + rnd.NextDouble() * 20; // 100‑120 ms
            detector.AddSample(latency);
        }
        sw.Stop();
        Console.WriteLine($"Processed {samples:N0} samples in {sw.ElapsedMilliseconds} ms (≈{samples / (sw.Elapsed.TotalSeconds):N0} samples/s)");
    }

    static void Main()
    {
        try
        {
            TestNormalOperation();
            TestConsecutiveHighLatency();
            TestNonConsecutiveHighLatency();
            TestResetFunctionality();
            Console.WriteLine("All unit tests passed.");

            Benchmark();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }
}
