using System;
using System.Collections.Generic;

namespace CapSimulation
{
    public enum ConsistencyLevel
    {
        Strong,
        Eventual
    }

    public sealed class PartitionException : Exception
    {
        public PartitionException(string message) : base(message) { }
    }

    public sealed class VersionedValue
    {
        public string Value { get; }
        public long Version { get; }

        public VersionedValue(string value, long version)
        {
            Value = value;
            Version = version;
        }
    }

    public sealed class Node
    {
        public int Id { get; }
        private readonly Dictionary<string, VersionedValue> _store = new();

        public Node(int id)
        {
            Id = id;
        }

        public void Write(string key, string value, long version)
        {
            _store[key] = new VersionedValue(value, version);
        }

        public VersionedValue? Read(string key)
        {
            _store.TryGetValue(key, out var vv);
            return vv;
        }

        public IReadOnlyDictionary<string, VersionedValue> Snapshot()
        {
            return new Dictionary<string, VersionedValue>(_store);
        }

        public void LoadSnapshot(IReadOnlyDictionary<string, VersionedValue> snapshot)
        {
            _store.Clear();
            foreach (var kvp in snapshot)
                _store[kvp.Key] = kvp.Value;
        }
    }

    public sealed class DistributedStore
    {
        private readonly List<Node> _nodes;
        private HashSet<int> _reachableNodeIds;
        private long _globalVersion = 0;

        public DistributedStore(IEnumerable<Node> nodes)
        {
            _nodes = new List<Node>(nodes);
            _reachableNodeIds = new HashSet<int>(_nodes.ConvertAll(n => n.Id));
        }

        public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();

        public long NextVersion() => ++_globalVersion;

        public void SetReachableNodes(IEnumerable<int> reachableIds)
        {
            _reachableNodeIds = new HashSet<int>(reachableIds);
        }

        public IEnumerable<Node> ReachableNodes => _nodes.FindAll(n => _reachableNodeIds.Contains(n.Id));

        public bool IsFullyConnected => _reachableNodeIds.Count == _nodes.Count;

        public void SyncAllNodes()
        {
            // Determine the latest version for each key across all nodes
            var latest = new Dictionary<string, VersionedValue>();

            foreach (var node in _nodes)
            {
                foreach (var kvp in node.Snapshot())
                {
                    if (!latest.TryGetValue(kvp.Key, out var existing) || kvp.Value.Version > existing.Version)
                    {
                        latest[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Propagate latest values to all nodes
            foreach (var node in _nodes)
            {
                node.LoadSnapshot(latest);
            }
        }
    }
}
