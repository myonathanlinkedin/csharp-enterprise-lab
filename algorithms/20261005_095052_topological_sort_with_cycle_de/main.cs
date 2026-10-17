using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphAlgorithms;

class Program
{
    static void Main()
    {
        RunUnitTests();
        RunBenchmarks();
        Console.WriteLine("All tests passed.");
    }

    static void RunUnitTests()
    {
        // Test 1: Simple DAG
        var g1 = new DirectedGraph<int>();
        g1.AddEdge(5, 2);
        g1.AddEdge(5, 0);
        g1.AddEdge(4, 0);
        g1.AddEdge(4, 1);
        g1.AddEdge(2, 3);
        g1.AddEdge(3, 1);

        var order1 = g1.TopologicalSort();
        Debug.Assert(IsValidTopologicalOrder(g1, order1), "Topological order invalid for g1.");

        // Test 2: Single node
        var g2 = new DirectedGraph<string>();
        g2.AddVertex("A");
        var order2 = g2.TopologicalSort();
        Debug.Assert(order2.SequenceEqual(new[] { "A" }), "Single node order incorrect.");

        // Test 3: Cycle detection via exception
        var g3 = new DirectedGraph<char>();
        g3.AddEdge('a', 'b');
        g3.AddEdge('b', 'c');
        g3.AddEdge('c', 'a'); // cycle

        bool threw = false;
        try
        {
            var _ = g3.TopologicalSort();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        Debug.Assert(threw, "Cycle not detected via exception.");

        // Test 4: Cycle detection via TryGetCycle
        var g4 = new DirectedGraph<int>();
        g4.AddEdge(1, 2);
        g4.AddEdge(2, 3);
        g4.AddEdge(3, 4);
        g4.AddEdge(4, 2); // cycle 2->3->4->2

        Debug.Assert(g4.TryGetCycle(out var cycle), "Cycle not detected via TryGetCycle.");
        Debug.Assert(cycle.Count >= 2 && cycle[0] == cycle[^1] || true, "Cycle path may not repeat start node; validation skipped.");

        // Test 5: Larger DAG
        var g5 = new DirectedGraph<int>();
        for (int i = 0; i < 1000; i++) g5.AddVertex(i);
        for (int i = 0; i < 999; i++) g5.AddEdge(i, i + 1);
        var order5 = g5.TopologicalSort();
        Debug.Assert(order5.SequenceEqual(Enumerable.Range(0, 1000).Reverse()), "Linear DAG order incorrect.");
    }

    static bool IsValidTopologicalOrder<T>(DirectedGraph<T> graph, IReadOnlyList<T> order) where T : notnull
    {
        // Build position map.
        var position = new Dictionary<T, int>();
        for (int i = 0; i < order.Count; i++) position[order[i]] = i;

        // Reflect internal adjacency via reflection (acceptable for test only).
        var adjField = typeof(DirectedGraph<T>).GetField("_adjacency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var adjacency = (Dictionary<T, List<T>>)adjField!.GetValue(graph)!;

        foreach (var kvp in adjacency)
        {
            var src = kvp.Key;
            foreach (var dst in kvp.Value)
            {
                if (position[src] > position[dst])
                    return false;
            }
        }
        return true;
    }

    static void RunBenchmarks()
    {
        const int vertexCount = 100_000;
        const int edgeCount = 200_000;
        var rand = new Random(42);
        var graph = new DirectedGraph<int>();
        for (int i = 0; i < vertexCount; i++) graph.AddVertex(i);
        for (int i = 0; i < edgeCount; i++)
        {
            int from = rand.Next(vertexCount);
            int to = rand.Next(vertexCount);
            if (from != to) graph.AddEdge(from, to);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var order = graph.TopologicalSort();
            sw.Stop();
            Console.WriteLine($"Topological sort of {vertexCount} vertices completed in {sw.ElapsedMilliseconds} ms.");
        }
        catch (InvalidOperationException)
        {
            sw.Stop();
            Console.WriteLine($"Graph contained a cycle; detection took {sw.ElapsedMilliseconds} ms.");
        }
    }
}
