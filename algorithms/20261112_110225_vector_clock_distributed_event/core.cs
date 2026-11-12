using System;
using System.Collections.Generic;
using System.Linq;

namespace DistributedSystems
{
    /// <summary>
    /// Represents a vector clock for distributed event ordering.
    /// </summary>
    public sealed class VectorClock : IEquatable<VectorClock>
    {
        // Internal representation: node identifier → counter.
        private readonly Dictionary<string, long> _clock;

        /// <summary>
        /// Creates an empty vector clock.
        /// </summary>
        public VectorClock()
        {
            _clock = new Dictionary<string, long>();
        }

        /// <summary>
        /// Creates a vector clock from an existing dictionary.
        /// The dictionary is copied to guarantee immutability of the internal state.
        /// </summary>
        private VectorClock(Dictionary<string, long> source)
        {
            _clock = new Dictionary<string, long>(source);
        }

        /// <summary>
        /// Returns the counter for the given node, or 0 if the node is unknown.
        /// </summary>
        public long Get(string node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            return _clock.TryGetValue(node, out var value) ? value : 0L;
        }

        /// <summary>
        /// Increments the counter for the specified node and returns a new VectorClock.
        /// </summary>
        public VectorClock Increment(string node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var copy = new Dictionary<string, long>(_clock);
            copy[node] = Get(node) + 1;
            return new VectorClock(copy);
        }

        /// <summary>
        /// Merges this clock with another clock, returning a new clock whose counters are the element‑wise maximum.
        /// </summary>
        public VectorClock Merge(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var merged = new Dictionary<string, long>(_clock);
            foreach (var kvp in other._clock)
            {
                var current = Get(kvp.Key);
                merged[kvp.Key] = Math.Max(current, kvp.Value);
            }
            return new VectorClock(merged);
        }

        /// <summary>
        /// Determines the partial order relationship between this clock and another.
        /// </summary>
        public Relation Compare(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            bool thisLessOrEqual = true;
            bool otherLessOrEqual = true;

            var allKeys = new HashSet<string>(_clock.Keys);
            allKeys.UnionWith(other._clock.Keys);

            foreach (var key in allKeys)
            {
                var a = Get(key);
                var b = other.Get(key);
                if (a < b) thisLessOrEqual = false;
                if (a > b) otherLessOrEqual = false;
                if (!thisLessOrEqual && !otherLessOrEqual) return Relation.Concurrent;
            }

            if (thisLessOrEqual && otherLessOrEqual) return Relation.Equal;
            if (thisLessOrEqual) return Relation.Ancestor; // this ≤ other and not equal
            return Relation.Descendant; // other ≤ this and not equal
        }

        /// <summary>
        /// Returns a deep copy of this vector clock.
        /// </summary>
        public VectorClock Clone()
        {
            return new VectorClock(_clock);
        }

        /// <summary>
        /// Returns a string representation suitable for debugging.
        /// </summary>
        public override string ToString()
        {
            var entries = _clock
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}:{kvp.Value}");
            return $"{{{string.Join(", ", entries)}}}";
        }

        public bool Equals(VectorClock? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return Compare(other) == Relation.Equal;
        }

        public override bool Equals(object? obj) => Equals(obj as VectorClock);

        public override int GetHashCode()
        {
            // Order‑independent hash based on key/value pairs.
            int hash = 17;
            foreach (var kvp in _clock.OrderBy(k => k.Key))
            {
                hash = hash * 31 + kvp.Key.GetHashCode();
                hash = hash * 31 + kvp.Value.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Partial order relationship between two vector clocks.
        /// </summary>
        public enum Relation
        {
            /// <summary>Both clocks are identical.</summary>
            Equal,
            /// <summary>This clock happened before the other.</summary>
            Ancestor,
            /// <summary>This clock happened after the other.</summary>
            Descendant,
            /// <summary>The clocks are concurrent (no causal relationship).</summary>
            Concurrent
        }
    }
}
