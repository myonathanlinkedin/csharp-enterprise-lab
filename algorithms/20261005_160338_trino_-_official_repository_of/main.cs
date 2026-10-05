using System;
using System.Collections.Generic;
using System.Diagnostics;
using TrinoSim;

class Program
{
    static void Main()
    {
        // Setup sample data.
        var orders = new InMemoryTable(
            "orders",
            new[]
            {
                new Dictionary<string, object> { ["order_id"] = 1, ["customer"] = "Alice",   ["total"] = 120.5 },
                new Dictionary<string, object> { ["order_id"] = 2, ["customer"] = "Bob",     ["total"] = 75.0 },
                new Dictionary<string, object> { ["order_id"] = 3, ["customer"] = "Alice",   ["total"] = 200.0 },
                new Dictionary<string, object> { ["order_id"] = 4, ["customer"] = "Charlie", ["total"] = 50.0 }
            });

        var connector = new InMemoryConnector(new[] { orders });
        var engine = new ExecutionEngine(connector);

        // Unit test 1: simple projection.
        var q1 = new Query("orders", new[] { "order_id", "customer" });
        var r1 = engine.Execute(q1);
        Assert(r1.Count == 4, "Projection count mismatch");
        Assert(r1[0]["order_id"].Equals(1), "First row order_id mismatch");
        Assert(r1[0]["customer"].Equals("Alice"), "First row customer mismatch");

        // Unit test 2: filter on total > 100.
        var q2 = new Query(
            "orders",
            new[] { "order_id", "total" },
            row => Convert.ToDouble(row["total"]) > 100.0);
        var r2 = engine.Execute(q2);
        Assert(r2.Count == 2, "Filter count mismatch");
        var ids = new HashSet<int> { (int)r2[0]["order_id"], (int)r2[1]["order_id"] };
        Assert(ids.SetEquals(new[] { 1, 3 }), "Filter result IDs mismatch");

        // Unit test 3: filter with no matching rows.
        var q3 = new Query(
            "orders",
            new[] { "order_id" },
            row => row["customer"]!.Equals("NonExistent"));
        var r3 = engine.Execute(q3);
        Assert(r3.Count == 0, "Expected zero rows for non‑matching filter");

        // Benchmark: execute 100,000 simple scans.
        const int iterations = 100_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            // Reuse same query object to avoid allocation overhead.
            engine.Execute(q1);
        }
        sw.Stop();
        Console.WriteLine($"Executed {iterations} scans in {sw.ElapsedMilliseconds} ms ({iterations / (sw.Elapsed.TotalSeconds):N0} ops/sec)");

        Console.WriteLine("All tests passed.");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Assertion failed: " + message);
    }
}
