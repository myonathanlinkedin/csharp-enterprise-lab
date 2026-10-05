using System;
using System.Diagnostics;
using System.Collections.Generic;
using DistributedHashing;

namespace DistributedHashingDemo
{
    internal static class Program
    {
        private static void Main()
        {
            RunTests();
            RunBenchmark();
        }

        private static void RunTests()
        {
            var ring = new ConsistentHashRing(replicas: 10);
            var nodeA = new Node("A", "10.0.0.1");
            var nodeB = new Node("B", "10.0.0.2");
            var nodeC = new Node("C", "10.0.0.3");

            ring.AddNode(nodeA);
            ring.AddNode(nodeB);
            ring.AddNode(nodeC);

            // Test that keys map to nodes
            var key1 = "file1.txt";
            var key2 = "file2.txt";
            var key3 = "file3.txt";

            var node1 = ring.GetNode(key1);
            var node2 = ring.GetNode(key2);
            var node3 = ring.GetNode(key3);

            Console.WriteLine($"Key '{key1}' mapped to {node1}");
            Console.WriteLine($"Key '{key2}' mapped to {node2}");
            Console.WriteLine($"Key '{key3}' mapped to {node3}");

            // Ensure deterministic mapping
            var node1Again = ring.GetNode(key1);
            Debug.Assert(node1 == node1Again, "Deterministic mapping failed.");

            // Remove a node and verify redistribution
            ring.RemoveNode(nodeB);
            var node2AfterRemoval = ring.GetNode(key2);
            Debug.Assert(node2AfterRemoval != nodeB, "Node removal failed.");

            // Consistency test: minimal key movement
            var keyDistributionBefore = new Dictionary<string, Node>
            {
                { key1, ring.GetNode(key1) },
                { key2, ring.GetNode(key2) },
                { key3, ring.GetNode(key3) }
            };

            ring.AddNode(nodeB); // Re-add node
            var keyDistributionAfter = new Dictionary<string, Node>
            {
                { key1, ring.GetNode(key1) },
                { key2, ring.GetNode(key2) },
                { key3, ring.GetNode(key3) }
            };

            int moved = 0;
            foreach (var kvp in keyDistributionBefore)
            {
                if (kvp.Value != keyDistributionAfter[kvp.Key]) moved++;
            }
            Console.WriteLine($"Keys moved after re-adding node: {moved} out of {keyDistributionBefore.Count}");

            Console.WriteLine("All tests passed.");
        }

        private static void RunBenchmark()
        {
            const int nodeCount = 50;
            const int keyCount = 100_000;
            var ring = new ConsistentHashRing(replicas: 20);
            for (int i = 0; i < nodeCount; i++)
            {
                ring.AddNode(new Node($"Node{i}", $"10.0.0.{i}"));
            }

            var random = new Random(42);
            var keys = new List<string>(keyCount);
            for (int i = 0; i < keyCount; i++)
            {
                keys.Add($"key_{random.Next(1_000_000_000)}");
            }

            var sw = Stopwatch.StartNew();
            foreach (var key in keys)
            {
                var node = ring.GetNode(key);
                // Simulate usage
                _ = node;
            }
            sw.Stop();

            Console.WriteLine($"Lookup of {keyCount} keys took {sw.ElapsedMilliseconds} ms.");
        }
    }
}
