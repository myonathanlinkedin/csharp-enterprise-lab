using System;
using BipartiteMatching;

namespace BipartiteMatchingDemo
{
    class Program
    {
        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        static void TestEmptyGraph()
        {
            var g = new BipartiteGraph(0, 0);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 0, "Empty graph matching size");
            Assert(res.PairU.Length == 0, "Empty graph PairU length");
            Assert(res.PairV.Length == 0, "Empty graph PairV length");
        }

        static void TestSingleEdge()
        {
            var g = new BipartiteGraph(1, 1);
            g.AddEdge(0, 0);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 1, "Single edge matching size");
            Assert(res.PairU[0] == 0, "PairU[0] should be 0");
            Assert(res.PairV[0] == 0, "PairV[0] should be 0");
        }

        static void TestNoEdges()
        {
            var g = new BipartiteGraph(3, 3);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 0, "No edges matching size");
            for (int i = 0; i < 3; i++)
            {
                Assert(res.PairU[i] == -1, $"PairU[{i}] should be -1");
                Assert(res.PairV[i] == -1, $"PairV[{i}] should be -1");
            }
        }

        static void TestPerfectMatching()
        {
            var g = new BipartiteGraph(3, 3);
            g.AddEdge(0, 0);
            g.AddEdge(1, 1);
            g.AddEdge(2, 2);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 3, "Perfect matching size");
            for (int i = 0; i < 3; i++)
            {
                Assert(res.PairU[i] == i, $"PairU[{i}] should be {i}");
                Assert(res.PairV[i] == i, $"PairV[{i}] should be {i}");
            }
        }

        static void TestComplexGraph()
        {
            var g = new BipartiteGraph(3, 3);
            g.AddEdge(0, 0);
            g.AddEdge(0, 1);
            g.AddEdge(1, 0);
            g.AddEdge(2, 1);
            g.AddEdge(2, 2);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 3, "Complex graph matching size");
            // Verify consistency
            for (int u = 0; u < 3; u++)
            {
                int v = res.PairU[u];
                if (v != -1)
                {
                    Assert(res.PairV[v] == u, $"Inconsistent pair for u={u}, v={v}");
                }
            }
            for (int v = 0; v < 3; v++)
            {
                int u = res.PairV[v];
                if (u != -1)
                {
                    Assert(res.PairU[u] == v, $"Inconsistent pair for v={v}, u={u}");
                }
            }
        }

        static void TestUnequalSizes()
        {
            var g = new BipartiteGraph(4, 3);
            g.AddEdge(0, 0);
            g.AddEdge(1, 1);
            g.AddEdge(2, 2);
            g.AddEdge(3, 0);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 3, "Unequal sizes matching size");
            // Ensure no right vertex matched twice
            int[] matchCount = new int[3];
            foreach (int v in res.PairU)
            {
                if (v != -1) matchCount[v]++;
            }
            for (int i = 0; i < 3; i++)
                Assert(matchCount[i] <= 1, $"Right vertex {i} matched more than once");
        }

        static void TestDuplicateEdges()
        {
            var g = new BipartiteGraph(2, 2);
            g.AddEdge(0, 0);
            g.AddEdge(0, 0); // duplicate
            g.AddEdge(1, 1);
            var res = HopcroftKarp.Compute(g);
            Assert(res.MatchingSize == 2, "Duplicate edges matching size");
            Assert(res.PairU[0] == 0, "PairU[0] should be 0");
            Assert(res.PairU[1] == 1, "PairU[1] should be 1");
        }

        static void Main()
        {
            TestEmptyGraph();
            TestSingleEdge();
            TestNoEdges();
            TestPerfectMatching();
            TestComplexGraph();
            TestUnequalSizes();
            TestDuplicateEdges();
            Console.WriteLine("All tests passed.");
        }
    }
}
