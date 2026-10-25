using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DominationApprox
{
    internal static class Program
    {
        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new Exception(message);
        }

        private static void TestEmptyGraph()
        {
            var g = new Graph();
            var ds = DominationApproximator.ComputeDominatingSet(g);
            Assert(ds.Count == 0, "Empty graph should yield empty dominating set.");
        }

        private static void TestSingleVertex()
        {
            var g = new Graph();
            g.AddVertex(1);
            var ds = DominationApproximator.ComputeDominatingSet(g);
            Assert(ds.SetEquals(new HashSet<int> { 1 }), "Single vertex graph must have the vertex in the dominating set.");
        }

        private static void TestIsolatedVertices()
        {
            var g = new Graph();
            g.AddVertex(1);
            g.AddVertex(2);
            g.AddVertex(3);
            var ds = DominationApproximator.ComputeDominatingSet(g);
            Assert(ds.SetEquals(new HashSet<int> { 1, 2, 3 }), "All isolated vertices must be selected.");
        }

        private static void TestStarGraph()
        {
            // Center 0, leaves 1..5
            var g = new Graph();
            for (int i = 0; i <= 5; i++) g.AddVertex(i);
            for (int i = 1; i <= 5; i++) g.AddEdge(0, i);

            var ds = DominationApproximator.ComputeDominatingSet(g);
            // Optimal dominating set is {0}
            Assert(ds.Count == 1 && ds.Contains(0), "Star graph should be dominated by its center.");
        }

        private static void TestPathGraph()
        {
            // Path 0-1-2-3-4
            var g = new Graph();
            for (int i = 0; i <= 4; i++) g.AddVertex(i);
            for (int i = 0; i < 4; i++) g.AddEdge(i, i + 1);

            var ds = DominationApproximator.ComputeDominatingSet(g);
            // One optimal solution is {1,3}
            Assert(ds.Count <= 3, "Path graph should be dominated by at most 3 vertices.");
            // Verify domination property
            foreach (var v in g.Vertices)
            {
                bool dominated = ds.Contains(v);
                foreach (var nb in g.Neighbors(v))
                {
                    if (ds.Contains(nb)) { dominated = true; break; }
                }
                Assert(dominated, $"Vertex {v} is not dominated.");
            }
        }

        private static void TestCycleGraph()
        {
            // Cycle 0-1-2-3-0
            var g = new Graph();
            for (int i = 0; i < 4; i++) g.AddVertex(i);
            for (int i = 0; i < 4; i++) g.AddEdge(i, (i + 1) % 4);

            var ds = DominationApproximator.ComputeDominatingSet(g);
            // Minimum dominating set size is 2
            Assert(ds.Count <= 2, "Cycle of length 4 should be dominated by at most 2 vertices.");
            // Verify domination
            foreach (var v in g.Vertices)
            {
                bool dominated = ds.Contains(v);
                foreach (var nb in g.Neighbors(v))
                {
                    if (ds.Contains(nb)) { dominated = true; break; }
                }
                Assert(dominated, $"Vertex {v} is not dominated in cycle test.");
            }
        }

        private static void RunAllTests()
        {
            TestEmptyGraph();
            TestSingleVertex();
            TestIsolatedVertices();
            TestStarGraph();
            TestPathGraph();
            TestCycleGraph();
        }

        private static void Demo()
        {
            // Demonstration on a small random sparse graph
            var g = new Graph();
            for (int i = 0; i < 7; i++) g.AddVertex(i);
            g.AddEdge(0, 1);
            g.AddEdge(0, 2);
            g.AddEdge(1, 3);
            g.AddEdge(2, 4);
            g.AddEdge(3, 5);
            g.AddEdge(4, 5);
            g.AddEdge(5, 6);

            var ds = DominationApproximator.ComputeDominatingSet(g);
            Console.WriteLine("Dominating set for demo graph: {" + string.Join(", ", ds) + "}");
        }

        public static void Main()
        {
            try
            {
                RunAllTests();
                Console.WriteLine("All unit tests passed.");
                Demo();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failure: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
