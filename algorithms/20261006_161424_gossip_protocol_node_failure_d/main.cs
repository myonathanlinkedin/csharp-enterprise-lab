using System;
using System.Collections.Generic;
using GossipFailureDetector;

namespace GossipFailureDetector
{
    public static class TestRunner
    {
        public static void RunAll()
        {
            try
            {
                TestFullKnowledgePropagation();
                TestFailureDetection();
                Console.WriteLine("All tests passed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void TestFullKnowledgePropagation()
        {
            var node1 = new Node(1, new[] { 2 });
            var node2 = new Node(2, new[] { 1, 3 });
            var node3 = new Node(3, new[] { 2 });

            var network = new NetworkSimulator(new[] { node1, node2, node3 });

            for (int i = 0; i < 5; i++) network.Step();

            Assert(node1.KnownAlive.SetEquals(new[] { 1, 2, 3 }), "Node1 should know all nodes.");
            Assert(node2.KnownAlive.SetEquals(new[] { 1, 2, 3 }), "Node2 should know all nodes.");
            Assert(node3.KnownAlive.SetEquals(new[] { 1, 2, 3 }), "Node3 should know all nodes.");
        }

        private static void TestFailureDetection()
        {
            var node1 = new Node(1, new[] { 2 });
            var node2 = new Node(2, new[] { 1, 3 });
            var node3 = new Node(3, new[] { 2 });

            var network = new NetworkSimulator(new[] { node1, node2, node3 });

            for (int i = 0; i < 2; i++) network.Step();

            node3.IsActive = false; // Simulate failure

            for (int i = 0; i < 4; i++) network.Step();

            Assert(node1.KnownFailed.Contains(3), "Node1 should detect Node3 as failed.");
            Assert(node2.KnownFailed.Contains(3), "Node2 should detect Node3 as failed.");
            Assert(!node3.KnownFailed.Contains(3), "Node3 should not detect itself as failed.");
        }
    }

    public class Program
    {
        public static void Main()
        {
            TestRunner.RunAll();
        }
    }
}
