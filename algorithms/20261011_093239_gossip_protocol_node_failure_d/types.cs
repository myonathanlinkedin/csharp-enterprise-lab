using System;
using System.Collections.Generic;
using System.Linq;

namespace GossipFailureDetector
{
    public enum NodeStatus
    {
        Alive,
        Suspected,
        Failed
    }

    public class NodeInfo
    {
        public int Id { get; }
        public NodeStatus Status { get; set; }
        public int LastSeenRound { get; set; }

        public NodeInfo(int id, int lastSeenRound)
        {
            Id = id;
            Status = NodeStatus.Alive;
            LastSeenRound = lastSeenRound;
        }
    }

    public class GossipMessage
    {
        public Dictionary<int, int> KnownLastSeen { get; }

        public GossipMessage(Dictionary<int, int> knownLastSeen)
        {
            KnownLastSeen = new Dictionary<int, int>(knownLastSeen);
        }
    }

    public class Node
    {
        public int Id { get; }
        private readonly int _threshold;
        private readonly Dictionary<int, NodeInfo> _knownNodes;
        private readonly HashSet<int> _failedNodes;

        public Node(int id, int threshold)
        {
            Id = id;
            _threshold = threshold;
            _knownNodes = new Dictionary<int, NodeInfo>();
            _failedNodes = new HashSet<int>();
        }

        public IReadOnlyDictionary<int, NodeInfo> KnownNodes => _knownNodes;

        public IReadOnlyCollection<int> FailedNodes => _failedNodes;

        public void InitializeKnownNodes(IEnumerable<int> allNodeIds, int currentRound)
        {
            foreach (var nodeId in allNodeIds)
            {
                if (!_knownNodes.ContainsKey(nodeId))
                {
                    _knownNodes[nodeId] = new NodeInfo(nodeId, currentRound);
                }
            }
        }

        public virtual void SendGossipTo(Node target, int currentRound)
        {
            var msg = new GossipMessage(_knownNodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.LastSeenRound));
            target.ReceiveGossip(msg, currentRound);
        }

        public virtual void ReceiveGossip(GossipMessage msg, int currentRound)
        {
            foreach (var kvp in msg.KnownLastSeen)
            {
                if (_knownNodes.TryGetValue(kvp.Key, out var info))
                {
                    if (kvp.Value < info.LastSeenRound)
                    {
                        info.LastSeenRound = kvp.Value;
                        if (info.Status != NodeStatus.Alive)
                        {
                            info.Status = NodeStatus.Alive;
                        }
                    }
                }
                else
                {
                    _knownNodes[kvp.Key] = new NodeInfo(kvp.Key, kvp.Value);
                }
            }
            if (_knownNodes.TryGetValue(Id, out var selfInfo))
            {
                selfInfo.LastSeenRound = currentRound;
            }
            else
            {
                _knownNodes[Id] = new NodeInfo(Id, currentRound);
            }
        }

        public virtual void IncrementCountersAndDetectFailures(int currentRound)
        {
            foreach (var kvp in _knownNodes)
            {
                var nodeId = kvp.Key;
                var info = kvp.Value;
                if (nodeId == Id) continue;
                if (currentRound - info.LastSeenRound > _threshold)
                {
                    if (info.Status != NodeStatus.Failed)
                    {
                        info.Status = NodeStatus.Failed;
                        _failedNodes.Add(nodeId);
                    }
                }
                else
                {
                    if (info.Status == NodeStatus.Failed)
                    {
                        info.Status = NodeStatus.Alive;
                        _failedNodes.Remove(nodeId);
                    }
                }
            }
        }

        public virtual void IncrementOwnCounter(int currentRound)
        {
            if (_knownNodes.TryGetValue(Id, out var info))
            {
                info.LastSeenRound = currentRound;
            }
            else
            {
                _knownNodes[Id] = new NodeInfo(Id, currentRound);
            }
        }

        public NodeInfo GetNodeInfo(int nodeId)
        {
            return _knownNodes.TryGetValue(nodeId, out var info) ? info : null;
        }
    }
}
