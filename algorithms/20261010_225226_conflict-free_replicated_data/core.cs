using System;
using System.Collections.Generic;
using System.Linq;

namespace CRDT
{
    /// <summary>
    /// A Conflict-free Replicated Data Type (CRDT) implementing a PN‑Counter.
    /// Each replica maintains two grow‑only maps: one for increments (P) and one for decrements (N).
    /// Merges are performed by taking the element‑wise maximum, guaranteeing convergence.
    /// </summary>
    public sealed class PNCounter
    {
        private readonly string _replicaId;
        private readonly Dictionary<string, long> _p; // increments per replica
        private readonly Dictionary<string, long> _n; // decrements per replica

        /// <summary>
        /// Creates a new PN‑Counter for the given replica identifier.
        /// </summary>
        /// <param name="replicaId">Unique identifier of the replica (e.g., a node name or UUID).</param>
        public PNCounter(string replicaId)
        {
            if (string.IsNullOrWhiteSpace(replicaId))
                throw new ArgumentException("Replica identifier must be non‑empty.", nameof(replicaId));

            _replicaId = replicaId;
            _p = new Dictionary<string, long>();
            _n = new Dictionary<string, long>();
        }

        /// <summary>
        /// Increments the counter by the specified positive amount (default 1).
        /// </summary>
        public void Increment(long amount = 1)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Increment amount must be positive.");

            if (_p.TryGetValue(_replicaId, out var current))
                _p[_replicaId] = checked(current + amount);
            else
                _p[_replicaId] = amount;
        }

        /// <summary>
        /// Decrements the counter by the specified positive amount (default 1).
        /// </summary>
        public void Decrement(long amount = 1)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Decrement amount must be positive.");

            if (_n.TryGetValue(_replicaId, out var current))
                _n[_replicaId] = checked(current + amount);
            else
                _n[_replicaId] = amount;
        }

        /// <summary>
        /// Returns the current logical value of the counter (P total minus N total).
        /// </summary>
        public long Value => GetTotal(_p) - GetTotal(_n);

        private static long GetTotal(Dictionary<string, long> map)
        {
            long sum = 0;
            foreach (var v in map.Values)
                sum = checked(sum + v);
            return sum;
        }

        /// <summary>
        /// Merges the state of another PN‑Counter into this instance.
        /// The operation is commutative, associative and idempotent.
        /// </summary>
        /// <param name="other">Another counter whose state will be merged.</param>
        public void Merge(PNCounter other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            MergeMap(_p, other._p);
            MergeMap(_n, other._n);
        }

        private static void MergeMap(Dictionary<string, long> target, Dictionary<string, long> source)
        {
            foreach (var kv in source)
            {
                if (target.TryGetValue(kv.Key, out var existing))
                    target[kv.Key] = Math.Max(existing, kv.Value);
                else
                    target[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// Produces a deep copy of the current counter.
        /// Useful for testing merge semantics without side‑effects.
        /// </summary>
        public PNCounter Clone()
        {
            var copy = new PNCounter(_replicaId);
            foreach (var kv in _p) copy._p[kv.Key] = kv.Value;
            foreach (var kv in _n) copy._n[kv.Key] = kv.Value;
            return copy;
        }
    }
}
