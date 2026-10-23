using System;
using System.Diagnostics;
using RateLimiter;

namespace RateLimiterDemo
{
    // Simple mutable time provider for deterministic tests
    internal sealed class MockTimeProvider : ITimeProvider
    {
        private DateTimeOffset _now;
        public MockTimeProvider(DateTimeOffset start) => _now = start;
        public DateTimeOffset UtcNow => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }

    internal static class Tests
    {
        public static void RunAll()
        {
            TestSlidingWindowBasic();
            TestTokenBucketRefill();
            TestMergeBehaviour();
            Console.WriteLine("All tests passed.");
        }

        private static void TestSlidingWindowBasic()
        {
            var mock = new MockTimeProvider(DateTimeOffset.UtcNow);
            var limiter = new SlidingWindowRateLimiter(
                capacity: 5,
                window: TimeSpan.FromSeconds(10),
                refillTokensPerSecond: 0,
                timeProvider: mock);

            // Acquire up to capacity
            for (int i = 0; i < 5; i++)
                Debug.Assert(limiter.TryAcquire(), "Should acquire within capacity");

            // Next request must fail
            Debug.Assert(!limiter.TryAcquire(), "Should reject when capacity exceeded");

            // Advance beyond window, all slots become free
            mock.Advance(TimeSpan.FromSeconds(11));
            Debug.Assert(limiter.TryAcquire(), "Should accept after window elapsed");
        }

        private static void TestTokenBucketRefill()
        {
            var mock = new MockTimeProvider(DateTimeOffset.UtcNow);
            var limiter = new SlidingWindowRateLimiter(
                capacity: 10,
                window: TimeSpan.FromSeconds(60), // large enough not to interfere
                refillTokensPerSecond: 2,
                timeProvider: mock);

            // Consume 5 tokens
            Debug.Assert(limiter.TryAcquire(5), "Initial acquire of 5 tokens");
            Debug.Assert(Math.Abs(limiter.AvailableTokens - 5) < 1e-6, "5 tokens should remain");

            // Advance 3 seconds -> 6 tokens refilled, but max capacity is 10
            mock.Advance(TimeSpan.FromSeconds(3));
            Debug.Assert(Math.Abs(limiter.AvailableTokens - 10) < 1e-6, "Tokens should refill to capacity");

            // Consume all tokens
            Debug.Assert(limiter.TryAcquire(10), "Consume all tokens");
            Debug.Assert(Math.Abs(limiter.AvailableTokens) < 1e-6, "No tokens left");
        }

        private static void TestMergeBehaviour()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockA = new MockTimeProvider(baseTime);
            var mockB = new MockTimeProvider(baseTime);

            var limiterA = new SlidingWindowRateLimiter(
                capacity: 8,
                window: TimeSpan.FromSeconds(20),
                refillTokensPerSecond: 1,
                timeProvider: mockA);

            var limiterB = new SlidingWindowRateLimiter(
                capacity: 8,
                window: TimeSpan.FromSeconds(20),
                refillTokensPerSecond: 1,
                timeProvider: mockB);

            // A consumes 3 tokens at t0
            Debug.Assert(limiterA.TryAcquire(3), "A consumes 3");
            // B consumes 2 tokens at t0
            Debug.Assert(limiterB.TryAcquire(2), "B consumes 2");

            // Advance time on both by 5 seconds (refill 5 tokens each)
            mockA.Advance(TimeSpan.FromSeconds(5));
            mockB.Advance(TimeSpan.FromSeconds(5));

            // Merge B into A
            limiterA.Merge(limiterB);

            // After merge, total tokens used should be 5 (3+2) and 10 tokens refilled (5+5) capped at capacity 8
            // So available tokens = capacity - used = 8 - 5 = 3
            Debug.Assert(Math.Abs(limiterA.AvailableTokens - 3) < 1e-6, "Available tokens after merge");

            // Window count should be 5 (3 from A, 2 from B)
            Debug.Assert(limiterA.CurrentWindowCount == 5, "Window count after merge");

            // B should be empty after merge
            Debug.Assert(limiterB.CurrentWindowCount == 0, "B should have zero window count");
            Debug.Assert(Math.Abs(limiterB.AvailableTokens) < 1e-6, "B should have zero tokens");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Tests.RunAll();
        }
    }
}
