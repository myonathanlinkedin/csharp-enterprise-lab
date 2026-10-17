using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace GossipFailureDetector
{
    internal static class Program
    {
        private static void Main()
        {
            // Basic sanity test: three nodes all alive
            var nodeA = new Node();
            var nodeB = new Node();
            var nodeC = new Node();

            var engine = new GossipEngine(new[] { nodeA, nodeB, nodeC }, TimeSpan.FromSeconds(5), randomSeed: 123);
            var start = DateTime.UtcNow;

            // Run 5 steps, all nodes should be alive
            for (int i = 0; i < 5; i++)
            {
                engine.Step(start.AddSeconds(i));
            }

            Debug.Assert(nodeA.GetPeerStatus(nodeB.Id) == NodeStatus.Alive);
            Debug.Assert(nodeA.GetPeerStatus(nodeC.Id) == NodeStatus.Alive);
            Debug.Assert(nodeB.GetPeerStatus(nodeA.Id) == NodeStatus.Alive);
            Debug.Assert(nodeB.GetPeerStatus(nodeC.Id) == NodeStatus.Alive);
            Debug.Assert(nodeC.GetPeerStatus(nodeA.Id) == NodeStatus.Alive);
            Debug.Assert(nodeC.GetPeerStatus(nodeB.Id) == NodeStatus.Alive);

            // Simulate nodeB failure by removing it from gossip participation
            var activeNodes = new[] { nodeA, nodeC };
            var failingEngine = new GossipEngine(activeNodes, TimeSpan.FromSeconds(3), randomSeed: 456);
            var now = start.AddSeconds(10);

            // Run steps where nodeB does not gossip
            for (int i = 0; i < 5; i++)
            {
                failingEngine.Step(now.AddSeconds(i));
            }

            // After timeout, nodeA and nodeC should suspect nodeB
            Debug.Assert(nodeA.GetPeerStatus(nodeB.Id) == NodeStatus.Suspected, "NodeA should suspect NodeB");
            Debug.Assert(nodeC.GetPeerStatus(nodeB.Id) == NodeStatus.Suspected, "NodeC should suspect NodeB");

            // Ensure they still consider each other alive
            Debug.Assert(nodeA.GetPeerStatus(nodeC.Id) == NodeStatus.Alive);
            Debug.Assert(nodeC.GetPeerStatus(nodeA.Id) == NodeStatus.Alive);

            // Edge case: single node network never suspects itself
            var soloNode = new Node();
            var soloEngine = new GossipEngine(new[] { soloNode }, TimeSpan.FromSeconds(2));
            var soloNow = DateTime.UtcNow;
            for (int i = 0; i < 3; i++)
            {
                soloEngine.Step(soloNow.AddSeconds(i));
            }
            Debug.Assert(!soloNode.SuspectedPeers.Any(), "Solo node should have no suspected peers");

            Console.WriteLine("All assertions passed.");
        }
    }
}
