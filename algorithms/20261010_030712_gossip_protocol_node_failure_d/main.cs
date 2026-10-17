using System;
using System.Diagnostics;
using System.Linq;
using GossipFailureDetector;

namespace GossipFailureDetectorDemo
{
    internal static class Program
    {
        private static void Main()
        {
            // Test 1: Zero nodes – engine should handle gracefully.
            var engineZero = new GossipEngine(nodeCount: 0, failureThreshold: 3);
            engineZero.Run(5);
            Debug.Assert(engineZero.FailedNodes.Count == 0, "Zero-node engine must have no failures.");

            // Test 2: Single node – never fails because there are no peers.
            var engineSingle = new GossipEngine(nodeCount: 1, failureThreshold: 3);
            engineSingle.Run(10);
            Debug.Assert(engineSingle.FailedNodes.Count == 0, "Single node should never be marked failed.");
            Debug.Assert(engineSingle.GetNode(0).State == NodeState.Alive, "Single node must stay alive.");

            // Test 3: Failure detection in a small cluster.
            const int nodeCount = 5;
            const int failureThreshold = 3;
            var engine = new GossipEngine(nodeCount, failureThreshold, fanout: 2, randomSeed: 123);
            // Run a few steps with all nodes alive.
            engine.Run(4);
            // Simulate failure of node 2.
            var failedNode = engine.GetNode(2);
            failedNode.IsActive = false; // stops sending heartbeats
            failedNode.State = NodeState.Failed; // optional explicit state change

            // Run enough steps for other nodes to notice the silence.
            engine.Run(failureThreshold + 2); // exceed threshold

            // Verify that node 2 is observed as failed by the engine.
            Debug.Assert(engine.FailedNodes.Contains(2), "Node 2 must be detected as failed.");
            // Verify that all other nodes have updated their state accordingly.
            for (int i = 0; i < nodeCount; i++)
            {
                if (i == 2) continue;
                var n = engine.GetNode(i);
                Debug.Assert(n.State == NodeState.Alive, $"Node {i} should remain alive.");
            }

            // Test 4: Ensure that a node that recovers (re‑activated) is not automatically resurrected.
            // Reactivate node 2 but keep it marked as failed globally.
            failedNode.IsActive = true;
            failedNode.State = NodeState.Alive; // local view changes, but engine's failure set stays.
            engine.Run(failureThreshold + 1);
            Debug.Assert(engine.FailedNodes.Contains(2), "Node 2 must remain in the global failure set after re‑activation.");

            // All assertions passed.
            Console.WriteLine("All Gossip Failure Detector tests passed.");
        }
    }
}
