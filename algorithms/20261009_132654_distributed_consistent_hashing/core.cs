using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ConsistentHashing
{
    public class ConsistentHashRouter<TNode>
    {
        private readonly SortedDictionary<uint, TNode> _ring = new SortedDictionary<uint, TNode>();
        private readonly Dictionary<TNode, List<uint>> _nodeHashes = new Dictionary<TNode, List<uint>>();
        private readonly int _virtualNodeCount;
        private readonly HashAlgorithm _hashAlgorithm;

        public ConsistentHashRouter(int virtualNodeCount = 100)
        {
            if (virtualNodeCount <= 0) throw new ArgumentOutOfRangeException(nameof(virtualNodeCount));
            _virtualNodeCount = virtualNodeCount;
            _hashAlgorithm = MD5.Create();
        }

        public void AddNode(TNode node)
        {
            if (_nodeHashes.ContainsKey(node)) throw new InvalidOperationException("Node already added.");
            var hashes = new List<uint>();
            for (int i = 0; i < _virtualNodeCount; i++)
            {
                string virtualNodeKey = $"{node}-{i}";
                uint hash = ComputeHash(virtualNodeKey);
                _ring[hash] = node;
                hashes.Add(hash);
            }
            _nodeHashes[node] = hashes;
        }

        public void RemoveNode(TNode node)
        {
            if (!_nodeHashes.TryGetValue(node, out var hashes)) throw new InvalidOperationException("Node not found.");
            foreach (var hash in hashes)
            {
                _ring.Remove(hash);
            }
            _nodeHashes.Remove(node);
        }

        public TNode GetNode(string key)
        {
            if (_ring.Count == 0) throw new InvalidOperationException("No nodes in the ring.");
            uint hash = ComputeHash(key);
            var kvp = _ring.FirstOrDefault(kv => kv.Key >= hash);
            if (!kvp.Equals(default(KeyValuePair<uint, TNode>))) return kvp.Value;
            return _ring.First().Value;
        }

        private uint ComputeHash(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = _hashAlgorithm.ComputeHash(bytes);
            return BitConverter.ToUInt32(hashBytes, 0);
        }
    }
}
