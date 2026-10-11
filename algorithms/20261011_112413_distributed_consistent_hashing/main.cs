using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsistentHashing
{
    internal static class Test
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception("Assertion failed: " + message);
        }

        public static void RunAll()
        {
            // 1. Empty ring should throw.
            var emptyRing = new ConsistentHashRing<string>(replicas: 10);
            bool threw = false;
            try { emptyRing.GetNode("any"); } catch (InvalidOperationException) { threw = true; }
            Assert(threw, "GetNode on empty ring must throw.");

            // 2. Basic add and deterministic mapping.
            var ring = new ConsistentHashRing<string>(replicas: 20);
            ring.AddNode("NodeA");
            ring.AddNode("NodeB");
            ring.AddNode("NodeC");

            string testKey = "consistent-key";
            var first = ring.GetNode(testKey);
            var second = ring.GetNode(testKey);
            Assert(first == second, "Same key must map to same node.");

            // 3. Rough distribution test.
            var distribution = new Dictionary<string, int>();
            for (int i = 0; i < 1000; i++)
            {
                var node = ring.GetNode("key-" + i);
                if (!distribution.ContainsKey(node)) distribution[node] = 0;
                distribution[node]++;
            }
            Assert(distribution.Count == 3, "All three nodes should receive keys.");
            foreach (var kv in distribution)
                Assert(kv.Value > 0, $"Node {kv.Key} received zero keys.");

            // 4. Removal of a node.
            ring.RemoveNode("NodeB");
            for (int i = 0; i < 200; i++)
            {
                var node = ring.GetNode("key-" + i);
                Assert(node != "NodeB", "Removed node must never be returned.");
            }

            // 5. Adding duplicate node must fail.
            bool dup = false;
            try { ring.AddNode("NodeA"); } catch (InvalidOperationException) { dup = true; }
            Assert(dup, "Adding a duplicate node should throw.");

            // 6. Custom replica count (including zero default).
            var singleRing = new ConsistentHashRing<string>(replicas: 0);
            singleRing.AddNode("Solo", replicaCount: 1);
            var solo = singleRing.GetNode("anything");
            Assert(solo == "Solo", "Single node with one replica must be returned.");

            // 7. Null key handling.
            bool nullThrown = false;
            try { singleRing.GetNode(null); } catch (ArgumentNullException) { nullThrown = true; }
            Assert(nullThrown, "GetNode(null) must throw ArgumentNullException.");

            Console.WriteLine("All tests passed.");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Test.RunAll();
        }
    }
}
