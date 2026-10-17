using System;
using System.Collections.Generic;

namespace KruskalMST
{
    internal static class TestRunner
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        }

        public static void RunAll()
        {
            TestSimpleGraph();
            TestNegativeWeights();
            TestDisconnectedGraph();
            TestSingleVertex();
            TestDuplicateEdges();
            Console.WriteLine("All Kruskal MST tests passed.");
        }

        private static void TestSimpleGraph()
        {
            // Graph:
            // 0---1 (1)
            // 0---2 (3)
            // 1---2 (1)
            // 1---3 (4)
            // 2---3 (2)
            var edges = new List<Edge>
            {
                new Edge(0,1,1),
                new Edge(0,2,3),
                new Edge(1,2,1),
                new Edge(1,3,4),
                new Edge(2,3,2)
            };
            var (mst, total) = KruskalAlgorithm.ComputeMST(4, edges);
            Assert(mst.Count == 3, "MST edge count");
            Assert(total == 4, $"Expected total weight 4, got {total}");
            // Expected edges: (0,1,1), (1,2,1), (2,3,2) in any order
            var expectedSet = new HashSet<(int,int,long)>
            {
                (0,1,1),(1,2,1),(2,3,2)
            };
            foreach (var e in mst)
            {
                var key = (Math.Min(e.Source,e.Target), Math.Max(e.Source,e.Target), e.Weight);
                Assert(expectedSet.Contains(key), $"Unexpected edge {e.Source}-{e.Target}:{e.Weight}");
                expectedSet.Remove(key);
            }
            Assert(expectedSet.Count == 0, "Missing expected edges");
        }

        private static void TestNegativeWeights()
        {
            // Triangle with negative edge
            var edges = new List<Edge>
            {
                new Edge(0,1,2),
                new Edge(1,2,-5),
                new Edge(0,2,4)
            };
            var (mst, total) = KruskalAlgorithm.ComputeMST(3, edges);
            Assert(mst.Count == 2, "MST edge count (negative)");
            Assert(total == -3, $"Expected total -3, got {total}");
        }

        private static void TestDisconnectedGraph()
        {
            var edges = new List<Edge>
            {
                new Edge(0,1,1),
                new Edge(2,3,2)
            };
            bool threw = false;
            try
            {
                KruskalAlgorithm.ComputeMST(4, edges);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "Disconnected graph should throw");
        }

        private static void TestSingleVertex()
        {
            var edges = new List<Edge>();
            var (mst, total) = KruskalAlgorithm.ComputeMST(1, edges);
            Assert(mst.Count == 0, "MST of single vertex should have 0 edges");
            Assert(total == 0, "Total weight of single vertex MST should be 0");
        }

        private static void TestDuplicateEdges()
        {
            // Parallel edges between 0 and 1, choose the lighter one
            var edges = new List<Edge>
            {
                new Edge(0,1,10),
                new Edge(0,1,3),
                new Edge(1,2,5),
                new Edge(0,2,8)
            };
            var (mst, total) = KruskalAlgorithm.ComputeMST(3, edges);
            Assert(mst.Count == 2, "MST edge count (duplicates)");
            Assert(total == 8, $"Expected total 8, got {total}");
            // Expected edges: (0,1,3) and (1,2,5)
            var expected = new HashSet<(int,int,long)>
            {
                (0,1,3),(1,2,5)
            };
            foreach (var e in mst)
            {
                var key = (Math.Min(e.Source,e.Target), Math.Max(e.Source,e.Target), e.Weight);
                Assert(expected.Contains(key), $"Unexpected edge {e.Source}-{e.Target}:{e.Weight}");
                expected.Remove(key);
            }
            Assert(expected.Count == 0, "Missing expected edge in duplicate test");
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            TestRunner.RunAll();
        }
    }
}
