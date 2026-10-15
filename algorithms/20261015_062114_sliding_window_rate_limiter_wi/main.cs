using System;
using RateLimiter;

namespace RateLimiterTest
{
    public sealed class FakeClock : IClock
    {
        private long _nowMs;
        public FakeClock(long startMs = 0) => _nowMs = startMs;
        public long NowMs() => _nowMs;
        public void Advance(TimeSpan ts) => _nowMs += (long)ts.TotalMilliseconds;
    }

    public static class Tests
    {
        public static void RunAll()
        {
            TestSlidingWindowBasic();
            TestSlidingWindowWindowExpiry();
            TestSlidingWindowEdgeCases();
            TestTokenBucketBasic();
            TestTokenBucketRefill();
            TestTokenBucketEdgeCases();
            Console.WriteLine("All tests passed.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void TestSlidingWindowBasic()
        {
            var clock = new FakeClock();
            var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromMilliseconds(100), clock);
            Assert(limiter.Allow(), "First request should be allowed");
            Assert(limiter.Allow(), "Second request should be allowed");
            Assert(limiter.Allow(), "Third request should be allowed");
            Assert(!limiter.Allow(), "Fourth request should be denied");
        }

        private static void TestSlidingWindowWindowExpiry()
        {
            var clock = new FakeClock();
            var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromMilliseconds(100), clock);
            Assert(limiter.Allow(), "First request allowed");
            Assert(limiter.Allow(), "Second request allowed");
            Assert(!limiter.Allow(), "Third denied");
            clock.Advance(TimeSpan.FromMilliseconds(101));
            Assert(limiter.Allow(), "After window expiry, request allowed");
        }

        private static void TestSlidingWindowEdgeCases()
        {
            try { new SlidingWindowRateLimiter(0, TimeSpan.FromMilliseconds(100), null); Assert(false, "Zero maxRequests should throw"); } catch (ArgumentOutOfRangeException) { }
            try { new SlidingWindowRateLimiter(1, TimeSpan.Zero, null); Assert(false, "Zero window should throw"); } catch (ArgumentOutOfRangeException) { }
        }

        private static void TestTokenBucketBasic()
        {
            var clock = new FakeClock();
            var limiter = new DistributedTokenBucketLimiter(5, TimeSpan.FromMilliseconds(100), clock);
            for (int i = 0; i < 5; i++)
                Assert(limiter.Allow(), $"Request {i+1} should be allowed");
            Assert(!limiter.Allow(), "Sixth request should be denied");
        }

        private static void TestTokenBucketRefill()
        {
            var clock = new FakeClock();
            var limiter = new DistributedTokenBucketLimiter(1, TimeSpan.FromMilliseconds(100), clock);
            Assert(limiter.Allow(), "First allowed");
            Assert(!limiter.Allow(), "Second denied");
            clock.Advance(TimeSpan.FromMilliseconds(100));
            Assert(limiter.Allow(), "After refill, request allowed");
            Assert(!limiter.Allow(), "Token bucket should have only one token after refill");
        }

        private static void TestTokenBucketEdgeCases()
        {
            try { new DistributedTokenBucketLimiter(0, TimeSpan.FromMilliseconds(100), null); Assert(false, "Zero capacity should throw"); } catch (ArgumentOutOfRangeException) { }
            try { new DistributedTokenBucketLimiter(1, TimeSpan.Zero, null); Assert(false, "Zero refill interval should throw"); } catch (ArgumentOutOfRangeException) { }
        }
    }

    public class Program
    {
        public static void Main()
        {
            Tests.RunAll();
        }
    }
}
