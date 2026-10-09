using System;
using System.Collections.Generic;
using EdgeRanking;

namespace EdgeRankingTest
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
            TestPathTree();
            TestStarTree();
            TestBalancedBinaryTree();
            TestRandomSmallTree();
        }

        // Helper to build a tree from edge list
        private static Tree BuildTree(int nodeCount, (int, int)[] edges)
        {
            var nodes = new List<int>();
            for (int i = 0; i < nodeCount; i++) nodes.Add(i);
            var edgeObjs = new List<Edge>();
            for (int i = 0; i < edges.Length; i++)
            {
                var (a, b) = edges[i];
                edgeObjs.Add(new Edge(i, a, b));
            }
            return new Tree(nodes, edgeObjs);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void TestPathTree()
        {
            // 0-1-2-3 (4 nodes, 3 edges)
            var tree = BuildTree(4, new[] { (0,1), (1,2), (2,3) });
            var ranking = EdgeRankingAlgorithm.ComputeEdgeRanking(tree);
            // Expected ranks: edge 0 (leaf) =1, edge1=2, edge2=1? Actually algorithm gives heights:
            // leaf edges get rank 1, internal edge gets higher rank.
            Assert(ranking.Count == 3, "PathTree: ranking count");
            Assert(EdgeRankingAlgorithm.VerifyRanking(tree, ranking), "PathTree: invalid ranking");
        }

        private static void TestStarTree()
        {
            // Center 0 connected to 1,2,3,4
            var tree = BuildTree(5, new[] { (0,1), (0,2), (0,3), (0,4) });
            var ranking = EdgeRankingAlgorithm.ComputeEdgeRanking(tree);
            // All edges are leaves => rank 1
            foreach (var r in ranking.Values) Assert(r == 1, "StarTree: expected rank 1");
            Assert(EdgeRankingAlgorithm.VerifyRanking(tree, ranking), "StarTree: invalid ranking");
        }

        private static void TestBalancedBinaryTree()
        {
            // Complete binary tree of height 2 (7 nodes)
            // edges: 0-1,0-2,1-3,1-4,2-5,2-6
            var tree = BuildTree(7, new[]
            {
                (0,1),(0,2),(1,3),(1,4),(2,5),(2,6)
            });
            var ranking = EdgeRankingAlgorithm.ComputeEdgeRanking(tree);
            Assert(ranking.Count == 6, "BinaryTree: ranking count");
            // Leaves edges should be rank 1, internal edges rank 2
            foreach (var kv in ranking)
            {
                var edge = tree.Edges[kv.Key];
                bool isLeaf = (edge.NodeA == 3 || edge.NodeA == 4 || edge.NodeA ==5 || edge.NodeA ==6 ||
                               edge.NodeB == 3 || edge.NodeB == 4 || edge.NodeB ==5 || edge.NodeB ==6);
                if (isLeaf) Assert(kv.Value == 1, "BinaryTree: leaf rank");
                else Assert(kv.Value == 2, "BinaryTree: internal rank");
            }
            Assert(EdgeRankingAlgorithm.VerifyRanking(tree, ranking), "BinaryTree: invalid ranking");
        }

        private static void TestRandomSmallTree()
        {
            // Random tree with 6 nodes
            // edges: 0-1,1-2,1-3,3-4,3-5
            var tree = BuildTree(6, new[]
            {
                (0,1),(1,2),(1,3),(3,4),(3,5)
            });
            var ranking = EdgeRankingAlgorithm.ComputeEdgeRanking(tree);
            Assert(ranking.Count == 5, "RandomTree: ranking count");
            Assert(EdgeRankingAlgorithm.VerifyRanking(tree, ranking), "RandomTree: invalid ranking");
        }
    }
}
