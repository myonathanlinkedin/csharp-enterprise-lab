using System;
using System.Collections.Generic;

namespace RateLimiter
{
    /// <summary>
    /// Sliding‑window rate limiter based on a token bucket.
    /// Allows at most <see cref="_maxRequests"/> acquisitions within a rolling <see cref="_window"/>.
    /// Thread‑safe and supports merging state from another limiter (useful for distributed scenarios).
    /// </summary>
    public sealed class SlidingWindowRateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private readonly Queue<DateTime> _timestamps = new();
        private readonly object _lock = new();

        /// <summary>
        /// Creates a new limiter.
        /// </summary>
        /// <param name="maxRequests">Maximum number of tokens allowed in the window.</param>
        /// <param name="window">Length of the sliding window.</param>
        public SlidingWindowRateLimiter(int maxRequests, TimeSpan window)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));

            _maxRequests = maxRequests;
            _window = window;
        }

        /// <summary>
        /// Attempts to acquire the specified number of tokens.
        /// </summary>
        /// <param name="tokens">Number of tokens to acquire (default 1).</param>
        /// <returns>True if the tokens were granted; otherwise false.</returns>
        public bool TryAcquire(int tokens = 1)
        {
            if (tokens <= 0) throw new ArgumentOutOfRangeException(nameof(tokens));

            var now = DateTime.UtcNow;
            lock (_lock)
            {
                PurgeOld(now);
                if (_timestamps.Count + tokens > _maxRequests)
                    return false;

                for (int i = 0; i < tokens; i++)
                    _timestamps.Enqueue(now);
                return true;
            }
        }

        /// <summary>
        /// Merges the state of another limiter into this one.
        /// The operation is commutative and safe for concurrent use.
        /// </summary>
        /// <param name="other">Another limiter with the same configuration.</param>
        public void Merge(SlidingWindowRateLimiter other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other._maxRequests != _maxRequests) throw new InvalidOperationException("Mismatched maxRequests.");
            if (other._window != _window) throw new InvalidOperationException("Mismatched window.");

            // Acquire locks in a deterministic order to avoid deadlock.
            var first = this.GetHashCode() < other.GetHashCode() ? this : other;
            var second = first == this ? other : this;

            lock (first._lock)
            {
                lock (second._lock)
                {
                    var now = DateTime.UtcNow;
                    // Merge timestamps.
                    foreach (var ts in other._timestamps)
                        _timestamps.Enqueue(ts);

                    PurgeOld(now);

                    // If we exceed capacity, keep the most recent timestamps.
                    while (_timestamps.Count > _maxRequests)
                    {
                        // Discard the oldest.
                        _timestamps.Dequeue();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of tokens currently consumed in the window.
        /// </summary>
        public int CurrentCount
        {
            get
            {
                lock (_lock)
                {
                    PurgeOld(DateTime.UtcNow);
                    return _timestamps.Count;
                }
            }
        }

        private void PurgeOld(DateTime now)
        {
            while (_timestamps.Count > 0 && (now - _timestamps.Peek()) > _window)
                _timestamps.Dequeue();
        }
    }
}
