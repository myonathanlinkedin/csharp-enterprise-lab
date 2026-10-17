using System;
using System.Linq;
using GossipFailureDetector;

namespace GossipFailureDetector
{
    public static class Program
    {
        public static void Main()
        {
            TestBasicFailureDetection();
            TestNoFalsePositives();
            TestHighThreshold();
            Console.WriteLine("All tests passed.");
        }

        private static void TestBasicFailureDetection()
        {
            var engine = new SimulationEngine(nodeCount: 5, threshold: 3);
            engine.RunRounds(5);
            engine.MarkNodeFailed(2);
            engine.RunRounds(5);
            foreach (var node in engine.Nodes)
            {
                if (node.Id == 2) continue;
                if (!node.FailedNodes.Contains(2))
                {
                    throw new Exception($"Node {node.Id} failed to detect node 2 as failed.");
                }
            }
        }

        private static void TestNoFalsePositives()
        {
            var engine = new SimulationEngine(nodeCount: 4, threshold: 4);
            engine.RunRounds(10);
            foreach (var node in engine.Nodes)
            {
                if (node.FailedNodes.Any())
                {
                    throw new Exception($"Node {node.Id} incorrectly detected failures.");
                }
            }
        }

        private static void TestHighThreshold()
        {
            var engine = new SimulationEngine(nodeCount: 3, threshold: 10);
            engine.MarkNodeFailed(1);
            engine.RunRounds(5);
            foreach (var node in engine.Nodes)
            {
                if (node.Id == 1) continue;
                if (node.FailedNodes.Contains(1))
                {
                    throw new Exception($"Node {node.Id} incorrectly detected node 1 as failed with high threshold.");
                }
            }
        }
    }
}
