using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ConsistentHashing;

namespace ConsistentHashingTest
{
    class Program
    {
        static void Main()
        {
            TestEmptyRing();
            TestAddAndGetNode();
            TestDuplicateAdd();
            TestRemoveNode();
            TestWrapAround();
            TestConsistentMapping();
            TestDistributionUniformity();
            BenchmarkLookup();
            Console.WriteLine("All tests passed.");
        }

        static void TestEmptyRing()
        {
            var router = new ConsistentHashRouter<string>();
            try
            {
                router.GetNode("key");
                throw new Exception("Expected exception for empty ring not thrown.");
            }
            catch (InvalidOperationException) { }
        }

        static void TestAddAndGetNode()
        {
            var router = new ConsistentHashRouter<string>(10);
            router.AddNode("A");
            router.AddNode("B");
            router.AddNode("C");
            var node = router.GetNode("my-key");
            if (node != "A" && node != "B" && node != "C")
                throw new Exception("GetNode returned unknown node.");
        }

        static void TestDuplicateAdd()
        {
            var router = new ConsistentHashRouter<string>(5);
            router.AddNode("X");
            try
            {
                router.AddNode("X");
                throw new Exception("Duplicate add did not throw.");
            }
            catch (InvalidOperationException) { }
        }

        static void TestRemoveNode()
        {
            var router = new ConsistentHashRouter<string>(5);
            router.AddNode("X");
            router.AddNode("Y");
            var nodeBefore = router.GetNode("key");
            router.RemoveNode("X");
            var nodeAfter = router.GetNode("key");
            if (nodeAfter == "X") throw new Exception("Removed node still returned.");
            if (nodeBefore == nodeAfter) throw new Exception("Node did not change after removal.");
        }

        static void TestWrapAround()
        {
            var router = new ConsistentHashRouter<string>(1);
            router.AddNode("First");
            // Force a key with hash greater than any node hash
            string highKey = new string('z', 1000);
            var node = router.GetNode(highKey);
            if (node != "First") throw new Exception("Wrap-around failed.");
        }

        static void TestConsistentMapping()
        {
            var router = new ConsistentHashRouter<string>(20);
            router.AddNode("A");
            router.AddNode("B");
            var first = router.GetNode("consistent-key");
            var second = router.GetNode("consistent-key");
            if (first != second) throw new Exception("Consistent mapping failed.");
        }

        static void TestDistributionUniformity()
        {
            var router = new ConsistentHashRouter<string>(50);
            var nodes = new[] { "N1", "N2", "N3", "N4", "N5" };
            foreach (var n in nodes) router.AddNode(n);
            var counts = nodes.ToDictionary(n => n, n => 0);
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                string key = $"key-{i}";
                var node = router.GetNode(key);
                counts[node]++;
            }
            double avg = total / (double)nodes.Length;
            foreach (var kvp in counts)
            {
                if (Math.Abs(kvp.Value - avg) > avg * 0.1)
                    throw new Exception($"Distribution not uniform for node {kvp.Key}.");
            }
        }

        static void BenchmarkLookup()
        {
            var router = new ConsistentHashRouter<string>(100);
            var nodes = Enumerable.Range(0, 10).Select(i => $"Node{i}").ToArray();
            foreach (var n in nodes) router.AddNode(n);
            var keys = Enumerable.Range(0, 100000).Select(i => $"Key{i}").ToArray();
            var sw = Stopwatch.StartNew();
            foreach (var key in keys)
            {
                var node = router.GetNode(key);
                if (node == null) throw new Exception("Lookup returned null.");
            }
            sw.Stop();
            Console.WriteLine($"Benchmark: {keys.Length} lookups in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
