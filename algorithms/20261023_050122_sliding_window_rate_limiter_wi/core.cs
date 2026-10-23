using System;
using System.Collections.Generic;
using System.Linq;

namespace RateLimiter
{
    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class SlidingWindowRateLimiter
    {
        private readonly int _capacity;
        private readonly TimeSpan _window;
        private readonly ITimeProvider _timeProvider;
        private readonly Queue<DateTimeOffset> _timestamps = new Queue<DateTimeOffset>();
        private readonly object _lock = new object();

        private readonly double _refillTokensPerSecond;
        private double _availableTokens;
        private DateTimeOffset _lastRefill;

        public SlidingWindowRateLimiter(int capacity, TimeSpan window, double refillTokensPerSecond, ITimeProvider timeProvider = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (window.TotalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(window));
            if (refillTokensPerSecond < 0) throw new ArgumentOutOfRangeException(nameof(refillTokensPerSecond));

            _capacity = capacity;
            _window = window;
            _refillTokensPerSecond = refillTokensPerSecond;
            _timeProvider = timeProvider ?? new SystemTimeProvider();

            _availableTokens = capacity;
            _lastRefill = _timeProvider.UtcNow;
        }

        internal void Refill()
        {
            var now = _timeProvider.UtcNow;
            var elapsedSeconds = (now - _lastRefill).TotalSeconds;
            if (elapsedSeconds <= 0) return;

            var added = elapsedSeconds * _refillTokensPerSecond;
            _availableTokens = Math.Min(_capacity, _availableTokens + added);
            _lastRefill = now;
        }

        public bool TryAcquire(int tokens = 1)
        {
            if (tokens <= 0) throw new ArgumentOutOfRangeException(nameof(tokens));

            lock (_lock)
            {
                // Token bucket refill
                Refill();
                if (_availableTokens < tokens) return false;

                var now = _timeProvider.UtcNow;

                // Remove stale timestamps
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                    _timestamps.Dequeue();

                if (_timestamps.Count + tokens > _capacity) return false;

                // Record acquisition
                for (int i = 0; i < tokens; i++)
                    _timestamps.Enqueue(now);

                _availableTokens -= tokens;
                return true;
            }
        }

        public void Merge(SlidingWindowRateLimiter other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other._capacity != _capacity ||
                other._window != _window ||
                Math.Abs(other._refillTokensPerSecond - _refillTokensPerSecond) > double.Epsilon)
                throw new InvalidOperationException("Incompatible limiter configurations.");

            // Acquire locks in a deterministic order to avoid deadlock
            var first = ReferenceEquals(this, other) ? this : (GetHashCode() < other.GetHashCode() ? this : other);
            var second = ReferenceEquals(first, this) ? other : this;

            lock (first._lock)
            {
                lock (second._lock)
                {
                    // Merge timestamps
                    var combined = new List<DateTimeOffset>(_timestamps);
                    combined.AddRange(other._timestamps);
                    combined.Sort();

                    var now = _timeProvider.UtcNow;
                    var fresh = new Queue<DateTimeOffset>();
                    foreach (var ts in combined)
                    {
                        if (now - ts <= _window)
                            fresh.Enqueue(ts);
                    }

                    _timestamps.Clear();
                    foreach (var ts in fresh) _timestamps.Enqueue(ts);
                    other._timestamps.Clear();

                    // Merge token bucket state
                    Refill();
                    other.Refill();

                    _availableTokens = Math.Min(_capacity, _availableTokens + other._availableTokens);
                    other._availableTokens = 0;

                    // Align refill timestamps
                    var latestRefill = _lastRefill > other._lastRefill ? _lastRefill : other._lastRefill;
                    _lastRefill = latestRefill;
                    other._lastRefill = latestRefill;
                }
            }
        }

        // Read‑only diagnostics for testing
        public int CurrentWindowCount
        {
            get
            {
                lock (_lock) return _timestamps.Count;
            }
        }

        public double AvailableTokens
        {
            get
            {
                lock (_lock)
                {
                    Refill();
                    return _availableTokens;
                }
            }
        }
    }
}
