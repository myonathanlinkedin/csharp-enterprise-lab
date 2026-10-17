using System;
using System.Collections.Generic;
using System.Linq;

namespace GossipFailureDetector
{
    public sealed class GossipEngine
    {
        private readonly List<Node> _nodes;
        private readonly int _failureThreshold; // steps after which a missing heartbeat => failure
        private readonly int _fanout; // number of peers each node gossips to per step
        private readonly Random _rng;
        private int _currentStep;
        private readonly HashSet<int> _failedNodes; // globally observed failures

        public IReadOnlyCollection<int> FailedNodes => _failedNodes;

        public GossipEngine(int nodeCount, int failureThreshold, int fanout = 2, int randomSeed = 42)
        {
            if (nodeCount < 0) throw new ArgumentOutOfRangeException(nameof(nodeCount));
            if (failureThreshold < 0) throw new ArgumentOutOfRangeException(nameof(failureThreshold));
            if (fanout < 0) throw new ArgumentOutOfRangeException(nameof(fanout));

            _nodes = new List<Node>(nodeCount);
            for (int i = 0; i < nodeCount; i++)
            {
                _nodes.Add(new Node(i, nodeCount));
            }

            _failureThreshold = failureThreshold;
            _fanout = fanout;
            _rng = new Random(randomSeed);
            _currentStep = 0;
            _failedNodes = new HashSet<int>();
        }

        // Executes a single logical time step: heartbeats are sent and failures are detected.
        public void Step()
        {
            _currentStep++;

            // 1. Each active node emits its heartbeat.
            foreach (var node in _nodes)
            {
                if (!node.IsActive) continue;
                node.LastHeartbeat = _currentStep;
                var peers = SelectRandomPeers(node.Id, _fanout);
                var msg = new GossipMessage(node.Id, _currentStep);
                foreach (var peerId in peers)
                {
                    var receiver = _nodes[peerId];
                    ProcessMessage(receiver, msg);
                }
            }

            // 2. After all messages have been processed, evaluate failures.
            DetectFailures();
        }

        // Returns a deterministic list of distinct peer IDs (excluding self).
        private List<int> SelectRandomPeers(int selfId, int count)
        {
            var candidateIds = _nodes
                .Where(n => n.Id != selfId && n.IsActive)
                .Select(n => n.Id)
                .ToList();

            var selected = new List<int>(Math.Min(count, candidateIds.Count));
            while (selected.Count < count && candidateIds.Count > 0)
            {
                int index = _rng.Next(candidateIds.Count);
                selected.Add(candidateIds[index]);
                candidateIds.RemoveAt(index);
            }

            return selected;
        }

        // Incorporates a received heartbeat into the receiver's view.
        private void ProcessMessage(Node receiver, GossipMessage msg)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            // Update only if the incoming counter is newer.
            if (msg.Counter > receiver.LastSeen[msg.FromId])
            {
                receiver.LastSeen[msg.FromId] = msg.Counter;
            }
        }

        // Marks nodes as failed if they have not been heard from within the threshold.
        private void DetectFailures()
        {
            foreach (var observer in _nodes)
            {
                foreach (var kvp in observer.LastSeen)
                {
                    int peerId = kvp.Key;
                    int lastSeenStep = kvp.Value;
                    if (_currentStep - lastSeenStep > _failureThreshold)
                    {
                        // Record failure globally.
                        _failedNodes.Add(peerId);
                        // Also update the peer's own state if we have a reference.
                        var peerNode = _nodes[peerId];
                        if (peerNode.State != NodeState.Failed)
                        {
                            peerNode.State = NodeState.Failed;
                            peerNode.IsActive = false; // stop sending further heartbeats
                        }
                    }
                }
            }
        }

        // Helper for unit tests: retrieve node by id.
        public Node GetNode(int id)
        {
            if (id < 0 || id >= _nodes.Count) throw new ArgumentOutOfRangeException(nameof(id));
            return _nodes[id];
        }

        // Runs the engine for a given number of steps.
        public void Run(int steps)
        {
            if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
            for (int i = 0; i < steps; i++)
            {
                Step();
            }
        }
    }
}
