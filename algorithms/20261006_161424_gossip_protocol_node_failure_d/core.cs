using System;
using System.Collections.Generic;
using System.Linq;

namespace GossipFailureDetector
{
    public class Node
    {
        public int Id { get; }
        public List<int> Neighbors { get; }
        public HashSet<int> KnownAlive { get; }
        public HashSet<int> KnownFailed { get; }
        public bool IsActive { get; set; } = true;

        private readonly Dictionary<int, int> _lastSeenRound;
        private int _currentRound;

        public const int FailureThreshold = 3;

        public Node(int id, IEnumerable<int> neighbors)
        {
            Id = id;
            Neighbors = new List<int>(neighbors);
            KnownAlive = new HashSet<int> { id };
            KnownFailed = new HashSet<int>();
            _lastSeenRound = new Dictionary<int, int> { { id, 0 } };
            _currentRound = 0;
        }

        public void IncrementRound()
        {
            _currentRound++;
            foreach (var kvp in _lastSeenRound.ToList())
            {
                if (kvp.Key == Id) continue;
                if (_currentRound - kvp.Value > FailureThreshold)
                {
                    KnownAlive.Remove(kvp.Key);
                    KnownFailed.Add(kvp.Key);
                }
            }
        }

        public GossipMessage CreateGossip()
        {
            return new GossipMessage(Id, new HashSet<int>(KnownAlive));
        }

        public void ReceiveGossip(GossipMessage msg)
        {
            if (msg.SenderId == Id) return;
            _lastSeenRound[msg.SenderId] = _currentRound;
            foreach (var node in msg.KnownAlive)
            {
                if (!KnownFailed.Contains(node))
                {
                    KnownAlive.Add(node);
                    _lastSeenRound[node] = _currentRound;
                }
            }
        }
    }

    public class GossipMessage
    {
        public int SenderId { get; }
        public HashSet<int> KnownAlive { get; }

        public GossipMessage(int senderId, HashSet<int> knownAlive)
        {
            SenderId = senderId;
            KnownAlive = knownAlive;
        }
    }

    public class NetworkSimulator
    {
        public Dictionary<int, Node> Nodes { get; }
        public int Round { get; private set; }

        public NetworkSimulator(IEnumerable<Node> nodes)
        {
            Nodes = nodes.ToDictionary(n => n.Id, n => n);
            Round = 0;
        }

        public void Step()
        {
            Round++;
            foreach (var node in Nodes.Values)
            {
                if (!node.IsActive) continue;
                var msg = node.CreateGossip();
                foreach (var neighborId in node.Neighbors)
                {
                    if (Nodes.TryGetValue(neighborId, out var neighbor))
                    {
                        neighbor.ReceiveGossip(msg);
                    }
                }
            }
            foreach (var node in Nodes.Values)
            {
                node.IncrementRound();
            }
        }
    }
}
