using System;
using System.Diagnostics;
using SSSPExample;

class Program
{
    static void Main()
    {
        TestDAGShortestPath();
        TestDialShortestPath();
        TestUnreachable();
        TestNegativeWeightException();
        TestNonDAGException();
        Console.WriteLine("All tests passed.");
    }

    static void AssertEqual(int[] a, int[] b)
    {
        if (a.Length != b.Length) throw new Exception("Array lengths differ.");
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i])
                throw new Exception($"Arrays differ at index {i}: {a[i]} != {b[i]}");
    }

    static void TestDAGShortestPath()
    {
        var g = new Graph(6);
        g.AddEdge(0, 1, 5);
        g.AddEdge(0, 2, 3);
        g.AddEdge(1, 3, 6);
        g.AddEdge(1, 2, 2);
        g.AddEdge(2, 4, 4);
        g.AddEdge(2, 5, 2);
        g.AddEdge(2, 3, 7);
        g.AddEdge(3, 4, -1); // negative weight allowed in DAG algorithm
        g.AddEdge(4, 5, -2);

        int[] expected = { 0, 5, 3, 8, 7, 5 };
        int[] result = SSSP.ShortestPathDAG(g, 0);
        AssertEqual(result, expected);
    }

    static void TestDialShortestPath()
    {
        var g = new Graph(5);
        g.AddEdge(0, 1, 1);
        g.AddEdge(0, 2, 4);
        g.AddEdge(1, 2, 2);
        g.AddEdge(1, 3, 5);
        g.AddEdge(2, 3, 1);
        g.AddEdge(3, 4, 3);

        int[] expected = { 0, 1, 3, 4, 7 };
        int[] result = SSSP.ShortestPathDial(g, 0, 5);
        AssertEqual(result, expected);
    }

    static void TestUnreachable()
    {
        var g = new Graph(3);
        g.AddEdge(0, 1, 2);
        // vertex 2 is unreachable
        int[] expected = { 0, 2, int.MaxValue };
        int[] result = SSSP.ShortestPathDial(g, 0, 2);
        AssertEqual(result, expected);
    }

    static void TestNegativeWeightException()
    {
        var g = new Graph(2);
        try
        {
            g.AddEdge(0, 1, -1);
            throw new Exception("Expected exception for negative weight not thrown.");
        }
        catch (ArgumentException) { /* expected */ }
    }

    static void TestNonDAGException()
    {
        var g = new Graph(3);
        g.AddEdge(0, 1, 1);
        g.AddEdge(1, 2, 1);
        g.AddEdge(2, 0, 1); // cycle
        try
        {
            SSSP.ShortestPathDAG(g, 0);
            throw new Exception("Expected exception for non-DAG not thrown.");
        }
        catch (InvalidOperationException) { /* expected */ }
    }
}
