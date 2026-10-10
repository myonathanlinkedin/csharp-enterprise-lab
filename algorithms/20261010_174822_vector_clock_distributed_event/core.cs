using System;
using System.Collections.Generic;
using System.Linq;

namespace VectorClockLib
{
    /// <summary>
    /// Represents the partial ordering relation between two vector clocks.
    /// </summary>
    public enum VectorClockRelation
    {
        LessThan,
        Equal,
        GreaterThan,
        Concurrent
    }

    /// <summary>
    /// A mutable vector clock implementation suitable for distributed event ordering.
    /// </summary>
    public sealed class VectorClock
    {
        // Internal representation: node identifier → counter.
        private readonly Dictionary<string, long> _clock = new Dictionary<string, long>(StringComparer.Ordinal);

        /// <summary>
        /// Increments the counter for the specified node (i.e., records a local event).
        /// </summary>
        /// <param name="nodeId">Unique identifier of the node.</param>
        public void Tick(string nodeId)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
            if (_clock.TryGetValue(nodeId, out long current))
                _clock[nodeId] = checked(current + 1);
            else
                _clock[nodeId] = 1;
        }

        /// <summary>
        /// Merges another vector clock into this one by taking the element‑wise maximum.
        /// </summary>
        /// <param name="other">The other vector clock to merge.</param>
        public void Merge(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            foreach (var kvp in other._clock)
            {
                if (_clock.TryGetValue(kvp.Key, out long current))
                    _clock[kvp.Key] = Math.Max(current, kvp.Value);
                else
                    _clock[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Determines the partial‑order relation between this clock and another.
        /// </summary>
        /// <param name="other">The clock to compare against.</param>
        /// <returns>A <see cref="VectorClockRelation"/> describing the relationship.</returns>
        public VectorClockRelation Compare(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            bool thisLess = false;
            bool thisGreater = false;

            // Union of keys from both clocks.
            var allKeys = new HashSet<string>(_clock.Keys, StringComparer.Ordinal);
            allKeys.UnionWith(other._clock.Keys);

            foreach (var key in allKeys)
            {
                _clock.TryGetValue(key, out long thisVal);
                other._clock.TryGetValue(key, out long otherVal);

                if (thisVal < otherVal) thisLess = true;
                else if (thisVal > otherVal) thisGreater = true;

                if (thisLess && thisGreater) return VectorClockRelation.Concurrent;
            }

            if (thisLess && !thisGreater) return VectorClockRelation.LessThan;
            if (thisGreater && !thisLess) return VectorClockRelation.GreaterThan;
            return VectorClockRelation.Equal;
        }

        /// <summary>
        /// Returns a read‑only snapshot of the clock's internal state.
        /// </summary>
        public IReadOnlyDictionary<string, long> Snapshot()
        {
            // Defensive copy to preserve encapsulation.
            return new Dictionary<string, long>(_clock);
        }

        public override bool Equals(object obj)
        {
            return obj is VectorClock other && Compare(other) == VectorClockRelation.Equal;
        }

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

        public override string ToString()
        {
            var parts = _clock
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}:{kvp.Value}");
            return $"{{{string.Join(", ", parts)}}}";
        }
    }
}
