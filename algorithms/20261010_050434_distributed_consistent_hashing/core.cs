using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ConsistentHashing
{
    /// <summary>
    /// Implements a consistent hashing ring with virtual nodes.
    /// </summary>
    public sealed class ConsistentHashRing
    {
        private readonly SortedDictionary<ulong, string> _ring = new SortedDictionary<ulong, string>();
        private readonly Dictionary<string, List<ulong>> _nodeMap = new Dictionary<string, List<ulong>>();
        private readonly int _replicas;
        private readonly MD5 _hasher = MD5.Create();

        /// <summary>
        /// Creates a new ring.
        /// </summary>
        /// <param name="replicas">Number of virtual nodes per physical node.</param>
        public ConsistentHashRing(int replicas = 100)
        {
            if (replicas <= 0) throw new ArgumentOutOfRangeException(nameof(replicas));
            _replicas = replicas;
        }

        /// <summary>
        /// Adds a physical node to the ring.
        /// </summary>
        public void AddNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node id cannot be null or empty.", nameof(nodeId));
            if (_nodeMap.ContainsKey(nodeId)) return; // idempotent

            var hashes = new List<ulong>(_replicas);
            for (int i = 0; i < _replicas; i++)
            {
                ulong hash = ComputeHash($"{nodeId}#{i}");
                // Collisions are extremely unlikely; resolve by linear probing.
                while (_ring.ContainsKey(hash))
                {
                    hash = (hash + 1) & ulong.MaxValue;
                }
                _ring[hash] = nodeId;
                hashes.Add(hash);
            }
            _nodeMap[nodeId] = hashes;
        }

        /// <summary>
        /// Removes a physical node and all its virtual nodes.
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node id cannot be null or empty.", nameof(nodeId));
            if (!_nodeMap.TryGetValue(nodeId, out var hashes)) return; // nothing to do

            foreach (var h in hashes)
            {
                _ring.Remove(h);
            }
            _nodeMap.Remove(nodeId);
        }

        /// <summary>
        /// Returns the node responsible for the supplied key.
        /// </summary>
        public string GetNode(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (_ring.Count == 0) throw new InvalidOperationException("Ring is empty.");

            ulong hash = ComputeHash(key);
            // Find first node with hash >= key hash.
            foreach (var kvp in _ring)
            {
                if (kvp.Key >= hash) return kvp.Value;
            }
            // Wrap around to the first node.
            foreach (var kvp in _ring)
            {
                return kvp.Value;
            }
            // Unreachable
            throw new InvalidOperationException("Failed to locate node.");
        }

        /// <summary>
        /// Returns a snapshot of current node identifiers.
        /// </summary>
        public IReadOnlyCollection<string> Nodes => _nodeMap.Keys;

        private ulong ComputeHash(string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] hash = _hasher.ComputeHash(data);
            // Use first 8 bytes for a 64‑bit hash.
            return BitConverter.ToUInt64(hash, 0);
        }
    }
}
