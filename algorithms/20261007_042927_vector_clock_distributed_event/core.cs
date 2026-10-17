using System;
using System.Collections.Generic;
using System.Linq;

namespace VectorClockDemo
{
    /// <summary>
    /// Represents a vector clock for distributed event ordering.
    /// </summary>
    public sealed class VectorClock
    {
        private readonly int _processId;
        private readonly Dictionary<int, long> _counters;

        public VectorClock(int processId)
        {
            if (processId < 0) throw new ArgumentOutOfRangeException(nameof(processId));
            _processId = processId;
            _counters = new Dictionary<int, long> { [processId] = 0 };
        }

        /// <summary>
        /// Gets the process identifier associated with this clock.
        /// </summary>
        public int ProcessId => _processId;

        /// <summary>
        /// Returns a shallow copy of the internal timestamp dictionary.
        /// </summary>
        public IReadOnlyDictionary<int, long> Timestamp => new Dictionary<int, long>(_counters);

        /// <summary>
        /// Increments the local counter and returns the new value.
        /// </summary>
        public long Increment()
        {
            _counters[_processId] = _counters.GetValueOrDefault(_processId) + 1;
            return _counters[_processId];
        }

        /// <summary>
        /// Merges another vector clock into this one by taking the element‑wise maximum.
        /// </summary>
        public void Merge(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            foreach (var kvp in other._counters)
            {
                long existing = _counters.GetValueOrDefault(kvp.Key);
                if (kvp.Value > existing)
                {
                    _counters[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Determines the partial ordering relation between this clock and another.
        /// </summary>
        public Relation Compare(VectorClock other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            bool less = false;
            bool greater = false;

            var allKeys = new HashSet<int>(_counters.Keys);
            allKeys.UnionWith(other._counters.Keys);

            foreach (int pid in allKeys)
            {
                long a = _counters.GetValueOrDefault(pid);
                long b = other._counters.GetValueOrDefault(pid);
                if (a < b) less = true;
                if (a > b) greater = true;
                if (less && greater) return Relation.Concurrent;
            }

            if (less && !greater) return Relation.LessThan;
            if (greater && !less) return Relation.GreaterThan;
            return Relation.Equal;
        }

        public override string ToString()
        {
            var parts = _counters.OrderBy(kvp => kvp.Key)
                                 .Select(kvp => $"{kvp.Key}:{kvp.Value}");
            return $"[{string.Join(", ", parts)}]";
        }
    }

    /// <summary>
    /// Partial ordering outcomes for vector clocks.
    /// </summary>
    public enum Relation
    {
        LessThan,
        Equal,
        GreaterThan,
        Concurrent
    }
}
