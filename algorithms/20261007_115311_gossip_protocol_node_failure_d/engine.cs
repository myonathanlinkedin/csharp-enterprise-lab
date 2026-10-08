using System;
using System.Collections.Generic;
using System.Linq;

namespace GossipFailureDetector
{
    public sealed class GossipEngine
    {
        private readonly List<Node> _nodes;
        private readonly TimeSpan _failureTimeout;
        private readonly Random _rand;

        public GossipEngine(IEnumerable<Node> nodes, TimeSpan failureTimeout, int randomSeed = 42)
        {
            _nodes = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
            if (_nodes.Count == 0) throw new ArgumentException("At least one node required.", nameof(nodes));
            _failureTimeout = failureTimeout;
            _rand = new Random(randomSeed);
        }

        // Simulate one gossip round
        public void Step(DateTime now)
        {
            // Each node increments its own heartbeat
            foreach (var node in _nodes)
            {
                node.Tick();
            }

            // Each node selects a random peer (if any) and exchanges gossip
            foreach (var node in _nodes)
            {
                var peers = _nodes.Where(n => n.Id != node.Id).ToList();
                if (peers.Count == 0) continue;

                var peer = peers[_rand.Next(peers.Count)];
                // Bidirectional exchange
                node.ReceiveGossip(peer, now);
                peer.ReceiveGossip(node, now);
            }

            // After exchange, evaluate suspicions
            foreach (var node in _nodes)
            {
                node.EvaluateSuspicions(_failureTimeout, now);
            }
        }

        // Helper to retrieve a node by id
        public Node GetNode(Guid id) => _nodes.FirstOrDefault(n => n.Id == id);

        // Exposes all nodes for testing
        public IReadOnlyCollection<Node> Nodes => _nodes;
    }
}
