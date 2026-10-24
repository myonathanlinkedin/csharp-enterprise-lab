using System;
using System.Collections.Generic;
using System.Diagnostics;
using ForkTrace;

class Program
{
    static void Main()
    {
        RunAllTests();
        Console.WriteLine("All ForkTrace tests passed.");
    }

    private static void RunAllTests()
    {
        TestSimpleChain();
        TestForkStructure();
        TestCycleHandling();
        TestIsolatedVertex();
        TestMultipleSources();
        TestNonExistentVertex();
    }

    private static void AssertEqual<T>(IReadOnlyCollection<T> actual, IReadOnlyCollection<T> expected, string message = "")
    {
        var a = new HashSet<T>(actual);
        var e = new HashSet<T>(expected);
        if (!a.SetEquals(e))
        {
            string err = $"Assertion failed. Expected: {{{string.Join(",", e)}}}, Actual: {{{string.Join(",", a)}}}";
            if (!string.IsNullOrEmpty(message)) err = $"{message} - {err}";
            throw new InvalidOperationException(err);
        }
    }

    // 1. Simple linear chain: 1 -> 2 -> 3 -> 4 (outbreak at 4)
    private static void TestSimpleChain()
    {
        var g = new Graph();
        g.AddEdge(1, 2);
        g.AddEdge(2, 3);
        g.AddEdge(3, 4);

        var sources = g.GetSources(4);
        AssertEqual(sources, new[] { 1 }, "Simple chain");
    }

    // 2. Fork structure:
    //      1
    //     / \
    //    2   3
    //     \ /
    //      4 (outbreak)
    private static void TestForkStructure()
    {
        var g = new Graph();
        g.AddEdge(1, 2);
        g.AddEdge(1, 3);
        g.AddEdge(2, 4);
        g.AddEdge(3, 4);

        var sources = g.GetSources(4);
        AssertEqual(sources, new[] { 1 }, "Fork structure");
    }

    // 3. Cycle handling:
    // 1 -> 2 -> 3 -> 1 (cycle) and 2 -> 4 (outbreak)
    private static void TestCycleHandling()
    {
        var g = new Graph();
        g.AddEdge(1, 2);
        g.AddEdge(2, 3);
        g.AddEdge(3, 1);
        g.AddEdge(2, 4);

        var sources = g.GetSources(4);
        // No vertex in the cycle has zero incoming edges, so only 4 itself is a source if it has no other incoming.
        // However 4 has incoming from 2, so the only source reachable upstream is none; algorithm returns empty set.
        AssertEqual(sources, new int[] { }, "Cycle handling");
    }

    // 4. Isolated vertex (no edges)
    private static void TestIsolatedVertex()
    {
        var g = new Graph();
        // No edges added; vertex 5 is isolated.
        // To make it known to the graph we add a self‑edge (optional) or rely on GetSources throwing.
        // We'll add a self‑edge to register it.
        g.AddEdge(5, 5);

        var sources = g.GetSources(5);
        // Since the only incoming edge is from itself, it is not a source; but the algorithm treats self‑loop as incoming.
        // Therefore result should be empty.
        AssertEqual(sources, new int[] { }, "Isolated vertex");
    }

    // 5. Multiple independent sources reaching the same outbreak node.
    // 1 -> 4, 2 -> 4, 3 -> 5 -> 4
    private static void TestMultipleSources()
    {
        var g = new Graph();
        g.AddEdge(1, 4);
        g.AddEdge(2, 4);
        g.AddEdge(3, 5);
        g.AddEdge(5, 4);

        var sources = g.GetSources(4);
        AssertEqual(sources, new[] { 1, 2, 3 }, "Multiple sources");
    }

    // 6. Requesting sources for a vertex that does not exist should throw.
    private static void TestNonExistentVertex()
    {
        var g = new Graph();
        g.AddEdge(1, 2);
        bool threw = false;
        try
        {
            var _ = g.GetSources(99);
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        if (!threw)
            throw new InvalidOperationException("Expected KeyNotFoundException for non‑existent vertex.");
    }
}
