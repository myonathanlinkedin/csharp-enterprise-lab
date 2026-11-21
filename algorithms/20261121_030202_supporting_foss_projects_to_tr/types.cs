using System.Collections.Generic;

namespace MemorySafeTech
{
    /// <summary>
    /// Represents a node/object in the simulated memory graph.
    /// </summary>
    public sealed class ObjectNode
    {
        /// <summary>Unique identifier for debugging and testing.</summary>
        public readonly int Id;

        /// <summary>Current external reference count (references from outside the graph).</summary>
        public int ExternalRefCount { get; private set; }

        /// <summary>Outgoing references to other nodes (edges in the graph).</summary>
        private readonly HashSet<ObjectNode> _outgoing = new();

        /// <summary>All nodes that reference this node (incoming edges).</summary>
        private readonly HashSet<ObjectNode> _incoming = new();

        public ObjectNode(int id)
        {
            Id = id;
            ExternalRefCount = 0;
        }

        /// <summary>
        /// Adds an outgoing reference from this node to <paramref name="target"/>.
        /// </summary>
        public void AddReference(ObjectNode target)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (_outgoing.Add(target))
            {
                target._incoming.Add(this);
            }
        }

        /// <summary>
        /// Removes an outgoing reference from this node to <paramref name="target"/>.
        /// </summary>
        public void RemoveReference(ObjectNode target)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (_outgoing.Remove(target))
            {
                target._incoming.Remove(this);
            }
        }

        /// <summary>
        /// Increment external reference count (e.g., a handle held by user code).
        /// </summary>
        public void IncrementExternal()
        {
            ExternalRefCount++;
        }

        /// <summary>
        /// Decrement external reference count. Throws if count would become negative.
        /// </summary>
        public void DecrementExternal()
        {
            if (ExternalRefCount == 0)
                throw new System.InvalidOperationException($"ExternalRefCount for node {Id} is already zero.");
            ExternalRefCount--;
        }

        /// <summary>
        /// Enumerates outgoing references.
        /// </summary>
        public IEnumerable<ObjectNode> Outgoing => _outgoing;

        /// <summary>
        /// Enumerates incoming references.
        /// </summary>
        public IEnumerable<ObjectNode> Incoming => _incoming;

        /// <summary>
        /// Clears all edges; used when the node is collected.
        /// </summary>
        public void ClearEdges()
        {
            foreach (var target in _outgoing)
            {
                target._incoming.Remove(this);
            }
            _outgoing.Clear();

            foreach (var source in _incoming)
            {
                source._outgoing.Remove(this);
            }
            _incoming.Clear();
        }
    }
}
