using System;
using System.Collections.Generic;
using System.Linq;

namespace GossipFailureDetector
{
    public class SimulationEngine
    {
        private readonly List<Node> _nodes;
        private readonly int _threshold;
        private readonly Random _random;
        private int _currentRound;

        public SimulationEngine(int nodeCount, int threshold, int randomSeed = 42)
        {
            if (nodeCount <= 0) throw new ArgumentException("nodeCount must be positive");
            _threshold = threshold;
            _random = new Random(randomSeed);
            _nodes = new List<Node>();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = new Node(i, _threshold);
                _nodes.Add(node);
            }
            var allIds = _nodes.Select(n => n.Id).ToList();
            foreach (var node in _nodes)
            {
                node.InitializeKnownNodes(allIds, _currentRound);
            }
        }

        public IReadOnlyList<Node> Nodes => _nodes;

        public int CurrentRound => _currentRound;

        public void RunRounds(int rounds)
        {
            if (rounds <= 0) throw new ArgumentException("rounds must be positive");
            for (int r = 0; r < rounds; r++)
            {
                _currentRound++;
                foreach (var node in _nodes)
                {
                    var peer = SelectRandomPeer(node);
                    if (peer != null)
                    {
                        node.SendGossipTo(peer, _currentRound);
                    }
                }
                foreach (var node in _nodes)
                {
                    node.IncrementOwnCounter(_currentRound);
                    node.IncrementCountersAndDetectFailures(_currentRound);
                }
            }
        }

        private Node SelectRandomPeer(Node node)
        {
            var possiblePeers = _nodes.Where(n => n.Id != node.Id).ToList();
            if (!possiblePeers.Any())
                return null;
            int index = _random.Next(possiblePeers.Count);
            return possiblePeers[index];
        }

        public void MarkNodeFailed(int nodeId)
        {
            var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                var dummy = new DummyNode(nodeId, _threshold);
                int idx = _nodes.IndexOf(node);
                _nodes[idx] = dummy;
            }
        }
    }

    internal class DummyNode : Node
    {
        public DummyNode(int id, int threshold) : base(id, threshold) { }

        public override void SendGossipTo(Node target, int currentRound) { }
        public override void ReceiveGossip(GossipMessage msg, int currentRound) { }
        public override void IncrementCountersAndDetectFailures(int currentRound) { }
        public override void IncrementOwnCounter(int currentRound) { }
    }
}
