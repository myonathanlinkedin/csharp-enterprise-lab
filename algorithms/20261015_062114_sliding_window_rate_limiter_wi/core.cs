using System;
using System.Collections.Generic;

namespace RateLimiter
{
    public interface IClock
    {
        long NowMs();
    }

    public sealed class SystemClock : IClock
    {
        public long NowMs() => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public sealed class SlidingWindowRateLimiter
    {
        private readonly int _maxRequests;
        private readonly long _windowMs;
        private readonly Queue<long> _timestamps = new Queue<long>();
        private readonly IClock _clock;

        public SlidingWindowRateLimiter(int maxRequests, TimeSpan window, IClock clock = null)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));
            _maxRequests = maxRequests;
            _windowMs = (long)window.TotalMilliseconds;
            _clock = clock ?? new SystemClock();
        }

        public bool Allow()
        {
            long now = _clock.NowMs();
            long threshold = now - _windowMs;
            while (_timestamps.Count > 0 && _timestamps.Peek() <= threshold)
            {
                _timestamps.Dequeue();
            }
            if (_timestamps.Count < _maxRequests)
            {
                _timestamps.Enqueue(now);
                return true;
            }
            return false;
        }
    }

    public sealed class DistributedTokenBucketLimiter
    {
        private readonly long _capacity;
        private readonly double _refillRatePerMs;
        private double _tokens;
        private long _lastRefillTimestamp;
        private readonly IClock _clock;

        public DistributedTokenBucketLimiter(long capacity, TimeSpan refillInterval, IClock clock = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (refillInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(refillInterval));
            _capacity = capacity;
            _refillRatePerMs = (double)capacity / refillInterval.TotalMilliseconds;
            _tokens = capacity;
            _clock = clock ?? new SystemClock();
            _lastRefillTimestamp = _clock.NowMs();
        }

        private void Refill()
        {
            long now = _clock.NowMs();
            long elapsed = now - _lastRefillTimestamp;
            if (elapsed > 0)
            {
                _tokens += elapsed * _refillRatePerMs;
                if (_tokens > _capacity) _tokens = _capacity;
                _lastRefillTimestamp = now;
            }
        }

        public bool Allow()
        {
            Refill();
            if (_tokens >= 1)
            {
                _tokens -= 1;
                return true;
            }
            return false;
        }
    }
}
