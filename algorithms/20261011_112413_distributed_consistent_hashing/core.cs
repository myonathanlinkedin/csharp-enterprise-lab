using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ConsistentHashing
{
    /// <summary>
    /// A thread‑safe consistent hashing ring with virtual nodes.
    /// </summary>
    /// <typeparam name="TNode">Type that identifies a physical node (e.g., string, Guid).</typeparam>
    public sealed class ConsistentHashRing<TNode>
    {
        private readonly SortedList<ulong, TNode> _ring = new SortedList<ulong, TNode>();
        private readonly Dictionary<TNode, List<ulong>> _nodeMap = new Dictionary<TNode, List<ulong>>();
        private readonly int _defaultReplicas;
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new ring.
        /// </summary>
        /// <param name="replicas">Default number of virtual nodes per physical node.</param>
        public ConsistentHashRing(int replicas = 100)
        {
            if (replicas < 0) throw new ArgumentOutOfRangeException(nameof(replicas));
            _defaultReplicas = replicas;
        }

        /// <summary>
        /// Adds a node using the default replica count.
        /// </summary>
        public void AddNode(TNode node) => AddNode(node, _defaultReplicas);

        /// <summary>
        /// Adds a node with a specific number of virtual replicas.
        /// </summary>
        public void AddNode(TNode node, int replicaCount)
        {
            if (replicaCount < 0) throw new ArgumentOutOfRangeException(nameof(replicaCount));
            lock (_lock)
            {
                if (_nodeMap.ContainsKey(node))
                    throw new InvalidOperationException("Node already exists in the ring.");

                var hashes = new List<ulong>(replicaCount);
                for (int i = 0; i < replicaCount; i++)
                {
                    string virtualId = $"{node}##{i}";
                    ulong hash = ComputeHash(virtualId);

                    // Resolve rare hash collisions by probing forward.
                    while (_ring.ContainsKey(hash))
                    {
                        i++;
                        virtualId = $"{node}##{i}";
                        hash = ComputeHash(virtualId);
                    }

                    _ring.Add(hash, node);
                    hashes.Add(hash);
                }

                _nodeMap[node] = hashes;
            }
        }

        /// <summary>
        /// Removes a node and all its virtual replicas.
        /// </summary>
        public void RemoveNode(TNode node)
        {
            lock (_lock)
            {
                if (!_nodeMap.TryGetValue(node, out var hashes))
                    throw new InvalidOperationException("Node not found in the ring.");

                foreach (var h in hashes)
                    _ring.Remove(h);

                _nodeMap.Remove(node);
            }
        }

        /// <summary>
        /// Returns the node responsible for the supplied key.
        /// </summary>
        public TNode GetNode(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                if (_ring.Count == 0)
                    throw new InvalidOperationException("Cannot locate a node in an empty ring.");

                ulong hash = ComputeHash(key);
                int idx = FindFirstGreaterOrEqual(hash);
                return _ring.Values[idx];
            }
        }

        /// <summary>
        /// Returns a snapshot of all physical nodes currently in the ring.
        /// </summary>
        public IReadOnlyCollection<TNode> GetAllNodes()
        {
            lock (_lock)
            {
                return _nodeMap.Keys.ToList().AsReadOnly();
            }
        }

        // Binary search for the first key >= target; wraps to 0 if none.
        private int FindFirstGreaterOrEqual(ulong target)
        {
            var keys = _ring.Keys;
            int low = 0, high = keys.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (keys[mid] < target)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            if (low == keys.Count) low = 0;
            return low;
        }

        // Deterministic 64‑bit hash based on MD5.
        private static ulong ComputeHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToUInt64(hash, 0);
            }
        }
    }
}
