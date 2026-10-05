using System;
using System.Diagnostics;
using GridTheoryLotSizing;

public static class Program
{
    private static int _testsPassed = 0;
    private static int _testsFailed = 0;

    private static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _testsPassed++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            _testsFailed++;
            Console.WriteLine($"[FAIL] {testName}");
        }
    }

    private static void RunTests()
    {
        Console.WriteLine("Running Unit Tests...");
        Console.WriteLine(new string('-', 40));

        // Test 1: Single period
        var p1 = new LotSizingProblem(1, new double[] { 10.0 }, new double[] { 1.0 }, new double[] { 5.0 });
        var s1 = GridTheorySolver.Solve(p1);
        Assert(Math.Abs(s1.TotalCost - 10.0) < 1e-9, "Single period cost");
        Assert(s1.OrderQuantities[0] == 5, "Single period quantity");

        // Test 2: Two periods, no holding
        var p2 = new LotSizingProblem(2, new double[] { 10.0, 10.0 }, new double[] { 0.0, 0.0 }, new double[] { 5.0, 5.0 });
        var s2 = GridTheorySolver.Solve(p2);
        Assert(Math.Abs(s2.TotalCost - 20.0) < 1e-9, "Two periods separate setups");

        // Test 3: Two periods, high holding cost favors separate orders
        var p3 = new LotSizingProblem(2, new double[] { 10.0, 10.0 }, new double[] { 100.0, 0.0 }, new double[] { 5.0, 5.0 });
        var s3 = GridTheorySolver.Solve(p3);
        Assert(Math.Abs(s3.TotalCost - 20.0) < 1e-9, "High holding cost separate orders");

        // Test 4: Two periods, low holding cost favors combined order
        var p4 = new LotSizingProblem(2, new double[] { 10.0, 10.0 }, new double[] { 0.1, 0.0 }, new double[] { 5.0, 5.0 });
        var s4 = GridTheorySolver.Solve(p4);
        Assert(Math.Abs(s4.TotalCost - 10.5) < 1e-9, "Low holding cost combined order");
        Assert(s4.OrderQuantities[0] == 10, "Combined order quantity");

        // Test 5: Three periods
        var p5 = new LotSizingProblem(3, new double[] { 10.0, 10.0, 10.0 }, new double[] { 1.0, 1.0, 1.0 }, new double[] { 5.0, 5.0, 5.0 });
        var s5 = GridTheorySolver.Solve(p5);
        Assert(s5.TotalCost > 0, "Three periods positive cost");

        // Test 6: Polynomiality metric
        double metric = GridTheorySolver.ComputePolynomialityMetric(p4, s4);
        Assert(metric >= 0, "Polynomiality metric non-negative");

        // Test 7: Zero demand
        var p7 = new LotSizingProblem(2, new double[] { 10.0, 10.0 }, new double[] { 1.0, 1.0 }, new double[] { 0.0, 0.0 });
        var s7 = GridTheorySolver.Solve(p7);
        Assert(Math.Abs(s7.TotalCost) < 1e-9, "Zero demand zero cost");

        // Test 8: Invalid input
        bool threwException = false;
        try
        {
            _ = new LotSizingProblem(0, new double[0], new double[0], new double[0]);
        }
        catch (ArgumentException)
        {
            threwException = true;
        }
        Assert(threwException, "Invalid N throws exception");

        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"Tests Passed: {_testsPassed}");
        Console.WriteLine($"Tests Failed: {_testsFailed}");
    }

    private static void RunBenchmark()
    {
        Console.WriteLine("\nRunning Benchmark...");
        int n = 1000;
        var rng = new Random(42);
        double[] setupCosts = new double[n];
        double[] holdingCosts = new double[n];
        double[] demands = new double[n];

        for (int i = 0; i < n; i++)
        {
            setupCosts[i] = 10.0 + rng.NextDouble() * 10.0;
            holdingCosts[i] = 0.1 + rng.NextDouble() * 0.5;
            demands[i] = 1.0 + rng.NextDouble() * 10.0;
        }

        var problem = new LotSizingProblem(n, setupCosts, holdingCosts, demands);
        var sw = Stopwatch.StartNew();
        var solution = GridTheorySolver.Solve(problem);
        sw.Stop();

        Console.WriteLine($"N = {n}");
        Console.WriteLine($"Total Cost: {solution.TotalCost:F2}");
        Console.WriteLine($"Elapsed Time: {sw.ElapsedMilliseconds} ms");
    }

    public static void Main(string[] args)
    {
        RunTests();
        RunBenchmark();
    }
}
