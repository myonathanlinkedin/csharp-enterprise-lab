using System;
using System.Collections.Generic;
using LocalCountingPathDetection;

namespace LocalCountingPathDetection
{
    internal static class Program
    {
        static void Main()
        {
            RunAllTests();
            Console.WriteLine("All tests passed.");
        }

        private static void RunAllTests()
        {
            TestEmptyGraph();
            TestSingleVertexLengthZero();
            TestSimplePathExists();
            TestSimplePathTooLong();
            TestChordedGraphInducedPath();
            TestLengthZeroOnEmptyGraph();
            TestNegativeLengthThrows();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void TestEmptyGraph()
        {
            var g = new Graph();
            Assert(!PathDetector.HasInducedPath(g, 1), "Empty graph should not contain a path of length 1.");
        }

        private static void TestSingleVertexLengthZero()
        {
            var g = new Graph();
            g.AddEdge(0, 0); // No effect, just ensure vertex 0 exists via a self‑loop attempt.
            g.AddEdge(0, 1); // Actually creates vertices 0 and 1; we will remove edge later.
            // Remove the edge by rebuilding a graph with a single isolated vertex.
            var single = new Graph();
            single.AddEdge(42, 42); // self‑loop ignored, yields isolated vertex 42.
            Assert(PathDetector.HasInducedPath(single, 0), "Graph with a single vertex should have an induced path of length 0.");
        }

        private static void TestSimplePathExists()
        {
            var g = new Graph();
            // Build path 1-2-3-4
            g.AddEdge(1, 2);
            g.AddEdge(2, 3);
            g.AddEdge(3, 4);
            Assert(PathDetector.HasInducedPath(g, 3), "Path 1-2-3-4 should be detected as induced length 3.");
        }

        private static void TestSimplePathTooLong()
        {
            var g = new Graph();
            // Same path 1-2-3-4
            g.AddEdge(1, 2);
            g.AddEdge(2, 3);
            g.AddEdge(3, 4);
            Assert(!PathDetector.HasInducedPath(g, 4), "No induced path of length 4 exists in a 4‑vertex line.");
        }

        private static void TestChordedGraphInducedPath()
        {
            var g = new Graph();
            // Build 1-2-3-4 with a chord 1-3
            g.AddEdge(1, 2);
            g.AddEdge(2, 3);
            g.AddEdge(3, 4);
            g.AddEdge(1, 3); // chord

            // Length 3 path (1-2-3-4) is NOT induced because of chord 1-3.
            Assert(!PathDetector.HasInducedPath(g, 3), "Chorded path of length 3 should be rejected.");

            // Length 2 induced path exists: 2-3-4 (no chord among {2,3,4})
            Assert(PathDetector.HasInducedPath(g, 2), "Induced path of length 2 should be found (2-3-4).");
        }

        private static void TestLengthZeroOnEmptyGraph()
        {
            var g = new Graph();
            Assert(!PathDetector.HasInducedPath(g, 0), "Empty graph should not have a length‑0 induced path.");
        }

        private static void TestNegativeLengthThrows()
        {
            var g = new Graph();
            g.AddEdge(1, 2);
            bool threw = false;
            try
            {
                PathDetector.HasInducedPath(g, -1);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            Assert(threw, "Negative length must throw ArgumentOutOfRangeException.");
        }
    }
}
