using System;
using System.Collections.Generic;

namespace MemorySafeTech
{
    /// <summary>
    /// Simulates a memory manager that tracks objects and performs deterministic garbage collection.
    /// </summary>
    public sealed class MemoryManager
    {
        private readonly Dictionary<int, ObjectNode> _nodes = new();

        /// <summary>
        /// Creates a new node with a unique identifier.
        /// </summary>
        public ObjectNode CreateNode(int id)
        {
            if (_nodes.ContainsKey(id))
                throw new InvalidOperationException($"Node with id {id} already exists.");
            var node = new ObjectNode(id);
            _nodes[id] = node;
            return node;
        }

        /// <summary>
        /// Retrieves a node by id; throws if not found.
        /// </summary>
        public ObjectNode GetNode(int id)
        {
            if (!_nodes.TryGetValue(id, out var node))
                throw new KeyNotFoundException($"Node with id {id} does not exist.");
            return node;
        }

        /// <summary>
        /// Adds an external reference to the node (simulating a handle held by user code).
        /// </summary>
        public void AddExternalReference(int nodeId)
        {
            var node = GetNode(nodeId);
            node.IncrementExternal();
        }

        /// <summary>
        /// Releases an external reference from the node.
        /// </summary>
        public void ReleaseExternalReference(int nodeId)
        {
            var node = GetNode(nodeId);
            node.DecrementExternal();
        }

        /// <summary>
        /// Adds an internal reference from <paramref name="sourceId"/> to <paramref name="targetId"/>.
        /// </summary>
        public void AddInternalReference(int sourceId, int targetId)
        {
            var source = GetNode(sourceId);
            var target = GetNode(targetId);
            source.AddReference(target);
        }

        /// <summary>
        /// Removes an internal reference from <paramref name="sourceId"/> to <paramref name="targetId"/>.
        /// </summary>
        public void RemoveInternalReference(int sourceId, int targetId)
        {
            var source = GetNode(sourceId);
            var target = GetNode(targetId);
            source.RemoveReference(target);
        }

        /// <summary>
        /// Performs a deterministic mark‑sweep collection.
        /// Nodes reachable from any node with ExternalRefCount > 0 survive;
        /// all others are reclaimed.
        /// </summary>
        public void CollectGarbage()
        {
            var visited = new HashSet<ObjectNode>();
            var stack = new Stack<ObjectNode>();

            // Seed with all roots (external references)
            foreach (var node in _nodes.Values)
            {
                if (node.ExternalRefCount > 0 && visited.Add(node))
                {
                    stack.Push(node);
                }
            }

            // Depth‑first traversal
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var child in current.Outgoing)
                {
                    if (visited.Add(child))
                    {
                        stack.Push(child);
                    }
                }
            }

            // Nodes not visited are unreachable and can be collected
            var toCollect = new List<int>();
            foreach (var kvp in _nodes)
            {
                var node = kvp.Value;
                if (!visited.Contains(node))
                {
                    // Clean edges before removal
                    node.ClearEdges();
                    toCollect.Add(kvp.Key);
                }
            }

            foreach (var id in toCollect)
            {
                _nodes.Remove(id);
            }
        }

        /// <summary>
        /// Returns the current count of managed nodes (useful for tests).
        /// </summary>
        public int NodeCount => _nodes.Count;
    }
}
