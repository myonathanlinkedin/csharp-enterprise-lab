using System;
using System.Collections.Generic;
using System.Linq;
using ConsistentHashing;

namespace ConsistentHashingTest
{
    internal static class TestHelper
    {
        public static void Assert(bool condition, string message = "Assertion failed.")
        {
            if (!condition) throw new Exception(message);
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            RunAllTests();
            Console.WriteLine("All tests passed.");
        }

        private static void RunAllTests()
        {
            TestBasicMapping();
            TestDeterminism();
            TestRemovalEffect();
            TestDistributionBalance();
        }

        private static void TestBasicMapping()
        {
            var ring = new ConsistentHashRing(replicas: 10);
            ring.AddNode("NodeA");
            ring.AddNode("NodeB");
            ring.AddNode("NodeC");

            var nodes = new HashSet<string> { "NodeA", "NodeB", "NodeC" };
            foreach (var key in new[] { "apple", "banana", "cherry", "date", "elderberry" })
            {
                string assigned = ring.GetNode(key);
                TestHelper.Assert(nodes.Contains(assigned), $"Key '{key}' mapped to unknown node '{assigned}'.");
            }
        }

        private static void TestDeterminism()
        {
            var ring = new ConsistentHashRing(replicas: 20);
            ring.AddNode("Alpha");
            ring.AddNode("Beta");
            ring.AddNode("Gamma");

            var key = "consistent-key-123";
            string first = ring.GetNode(key);
            for (int i = 0; i < 20; i++)
            {
                string later = ring.GetNode(key);
                TestHelper.Assert(first == later, "Consistent hashing returned different nodes for same key.");
            }
        }

        private static void TestRemovalEffect()
        {
            var ring = new ConsistentHashRing(replicas: 15);
            ring.AddNode("X");
            ring.AddNode("Y");
            ring.AddNode("Z");

            var key = "critical-key";
            string before = ring.GetNode(key);
            ring.RemoveNode(before);
            string after = ring.GetNode(key);
            TestHelper.Assert(before != after, "Key remained on removed node.");
            TestHelper.Assert(after != null, "After removal, key mapping returned null.");
        }

        private static void TestDistributionBalance()
        {
            const int nodeCount = 5;
            const int keyCount = 10000;
            const double tolerance = 0.20; // 20 % deviation allowed

            var ring = new ConsistentHashRing(replicas: 30);
            var nodeIds = Enumerable.Range(0, nodeCount).Select(i => $"Node{i}").ToArray();
            foreach (var id in nodeIds) ring.AddNode(id);

            var distribution = new Dictionary<string, int>();
            foreach (var id in nodeIds) distribution[id] = 0;

            var rng = new Random(42);
            for (int i = 0; i < keyCount; i++)
            {
                string key = "key-" + rng.Next();
                string node = ring.GetNode(key);
                distribution[node]++;
            }

            double expected = (double)keyCount / nodeCount;
            foreach (var kvp in distribution)
            {
                double ratio = Math.Abs(kvp.Value - expected) / expected;
                TestHelper.Assert(ratio <= tolerance,
                    $"Node {kvp.Key} deviates {ratio:P0} from expected distribution.");
            }
        }
    }
}
