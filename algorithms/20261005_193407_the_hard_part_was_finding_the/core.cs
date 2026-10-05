using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DistributedHashing
{
    /// <summary>
    /// Represents a node in the distributed hash ring.
    /// </summary>
    public sealed class Node
    {
        public string Id { get; }
        public string Address { get; }

        public Node(string id, string address)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        public override string ToString() => $"{Id}@{Address}";
    }

    /// <summary>
    /// Implements a consistent hashing ring with virtual nodes for load balancing.
    /// </summary>
    public sealed class ConsistentHashRing
    {
        private readonly SortedDictionary<uint, Node> _ring = new SortedDictionary<uint, Node>();
        private readonly int _replicas;
        private readonly HashAlgorithm _hashAlgorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistentHashRing"/> class.
        /// </summary>
        /// <param name="replicas">Number of virtual nodes per physical node.</param>
        public ConsistentHashRing(int replicas = 100)
        {
            if (replicas <= 0) throw new ArgumentOutOfRangeException(nameof(replicas));
            _replicas = replicas;
            _hashAlgorithm = SHA256.Create();
        }

        /// <summary>
        /// Adds a node to the ring.
        /// </summary>
        public void AddNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            for (int i = 0; i < _replicas; i++)
            {
                uint hash = ComputeHash($"{node.Id}#{i}");
                _ring[hash] = node;
            }
        }

        /// <summary>
        /// Removes a node and all its virtual nodes from the ring.
        /// </summary>
        public void RemoveNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var keysToRemove = _ring
                .Where(kvp => kvp.Value == node)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in keysToRemove)
            {
                _ring.Remove(key);
            }
        }

        /// <summary>
        /// Retrieves the node responsible for the given key.
        /// </summary>
        public Node GetNode(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            uint hash = ComputeHash(key);
            if (_ring.Count == 0) throw new InvalidOperationException("Hash ring is empty.");

            var tailMap = _ring.Where(kvp => kvp.Key >= hash);
            if (tailMap.Any())
                return tailMap.First().Value;

            // Wrap around to the first node
            return _ring.First().Value;
        }

        private uint ComputeHash(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = _hashAlgorithm.ComputeHash(bytes);
            // Use first 4 bytes for a 32-bit hash
            return BitConverter.ToUInt32(hashBytes, 0);
        }
    }
}
