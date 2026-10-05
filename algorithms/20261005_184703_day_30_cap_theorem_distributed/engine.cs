using System;
using System.Collections.Generic;
using System.Linq;

namespace CAPSimulation
{
    public class DistributedNode : IDistributedNode
    {
        public int Id { get; }
        private readonly Dictionary<string, (string value, long timestamp)> _store = new();

        public DistributedNode(int id)
        {
            Id = id;
        }

        public void ApplyWrite(string key, string value, long timestamp)
        {
            if (_store.TryGetValue(key, out var existing))
            {
                if (timestamp > existing.timestamp)
                {
                    _store[key] = (value, timestamp);
                }
            }
            else
            {
                _store[key] = (value, timestamp);
            }
        }

        public (string value, long timestamp) ReadLocal(string key)
        {
            return _store.TryGetValue(key, out var entry) ? entry : (null, -1);
        }

        public bool CanCommunicateWith(int otherNodeId, IEnumerable<Partition> partitions)
        {
            foreach (var p in partitions)
            {
                if (p.IsPartitioned(this.Id, otherNodeId))
                    return false;
            }
            return true;
        }
    }

    public class DistributedSystem
    {
        private readonly List<DistributedNode> _nodes;
        private readonly List<Partition> _partitions = new();

        private long _logicalClock = 0;

        public DistributedSystem(int nodeCount)
        {
            if (nodeCount < 1) throw new ArgumentException("At least one node required.");
            _nodes = Enumerable.Range(0, nodeCount).Select(i => new DistributedNode(i)).ToList();
        }

        public IReadOnlyList<DistributedNode> Nodes => _nodes;

        public void AddPartition(Partition partition)
        {
            _partitions.Add(partition);
        }

        public void ClearPartitions()
        {
            _partitions.Clear();
        }

        private IEnumerable<DistributedNode> ReachableNodes(DistributedNode from)
        {
            return _nodes.Where(n => n.Id == from.Id || from.CanCommunicateWith(n.Id, _partitions));
        }

        public void Write(string key, string value, ConsistencyModel consistency)
        {
            _logicalClock++;

            if (consistency == ConsistencyModel.Strong)
            {
                var majority = (_nodes.Count / 2) + 1;
                var candidateSets = GetMutuallyReachableSets();
                var suitableSet = candidateSets.FirstOrDefault(set => set.Count >= majority);
                if (suitableSet == null)
                    throw new UnavailableException("Cannot achieve quorum for strong consistency due to partition.");

                foreach (var node in suitableSet)
                {
                    node.ApplyWrite(key, value, _logicalClock);
                }
            }
            else // Eventual
            {
                foreach (var node in _nodes)
                {
                    node.ApplyWrite(key, value, _logicalClock);
                }
            }
        }

        public string Read(string key, ConsistencyModel consistency)
        {
            if (consistency == ConsistencyModel.Strong)
            {
                var majority = (_nodes.Count / 2) + 1;
                var candidateSets = GetMutuallyReachableSets();
                var suitableSet = candidateSets.FirstOrDefault(set => set.Count >= majority);
                if (suitableSet == null)
                    throw new UnavailableException("Cannot achieve quorum for strong read due to partition.");

                var latest = (value: (string)null, timestamp: -1L);
                foreach (var node in suitableSet)
                {
                    var (val, ts) = node.ReadLocal(key);
                    if (ts > latest.timestamp)
                    {
                        latest = (val, ts);
                    }
                }
                return latest.value;
            }
            else // Eventual
            {
                var node = _nodes[0];
                var (val, _) = node.ReadLocal(key);
                return val;
            }
        }

        private List<HashSet<DistributedNode>> GetMutuallyReachableSets()
        {
            var visited = new HashSet<int>();
            var sets = new List<HashSet<DistributedNode>>();

            foreach (var node in _nodes)
            {
                if (visited.Contains(node.Id)) continue;

                var component = new HashSet<DistributedNode>();
                var stack = new Stack<DistributedNode>();
                stack.Push(node);
                visited.Add(node.Id);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    component.Add(current);

                    foreach (var neighbor in _nodes)
                    {
                        if (visited.Contains(neighbor.Id)) continue;
                        if (current.CanCommunicateWith(neighbor.Id, _partitions))
                        {
                            visited.Add(neighbor.Id);
                            stack.Push(neighbor);
                        }
                    }
                }

                sets.Add(component);
            }

            return sets;
        }
    }
}
