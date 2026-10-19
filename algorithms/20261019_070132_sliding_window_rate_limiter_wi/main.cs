using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using RateLimiter;

class Program
{
    static void Main()
    {
        RunAllTests();
        Console.WriteLine("All tests passed.");
    }

    static void RunAllTests()
    {
        TestBasicLimit();
        TestSlidingWindowBehaviour();
        TestMergeBehaviour();
        TestConcurrencySafety();
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Assertion failed: " + message);
    }

    static void TestBasicLimit()
    {
        var limiter = new SlidingWindowRateLimiter(5, TimeSpan.FromSeconds(1));

        // Acquire exactly the limit.
        for (int i = 0; i < 5; i++)
            Assert(limiter.TryAcquire(), $"Basic limit: acquisition {i + 1} should succeed.");

        // Next acquisition must fail.
        Assert(!limiter.TryAcquire(), "Basic limit: acquisition beyond limit should fail.");

        // Wait for the window to expire.
        Thread.Sleep(1100);
        Assert(limiter.TryAcquire(), "Basic limit: after window expiry acquisition should succeed.");
    }

    static void TestSlidingWindowBehaviour()
    {
        var limiter = new SlidingWindowRateLimiter(5, TimeSpan.FromSeconds(2));

        // Acquire 3 tokens.
        for (int i = 0; i < 3; i++)
            Assert(limiter.TryAcquire(), $"Sliding window: first batch {i + 1} should succeed.");

        // Wait half the window.
        Thread.Sleep(1100);

        // Acquire 2 more tokens – should still be within limit.
        for (int i = 0; i < 2; i++)
            Assert(limiter.TryAcquire(), $"Sliding window: second batch {i + 1} should succeed.");

        // Now limit reached.
        Assert(!limiter.TryAcquire(), "Sliding window: further acquisition should fail.");

        // After the earliest three tokens expire, we should be able to acquire again.
        Thread.Sleep(1100);
        Assert(limiter.TryAcquire(), "Sliding window: after oldest tokens expire acquisition should succeed.");
    }

    static void TestMergeBehaviour()
    {
        var a = new SlidingWindowRateLimiter(5, TimeSpan.FromSeconds(1));
        var b = new SlidingWindowRateLimiter(5, TimeSpan.FromSeconds(1));

        // A consumes 3 tokens.
        for (int i = 0; i < 3; i++)
            Assert(a.TryAcquire(), $"Merge: A acquisition {i + 1} should succeed.");

        // B consumes 2 tokens.
        for (int i = 0; i < 2; i++)
            Assert(b.TryAcquire(), $"Merge: B acquisition {i + 1} should succeed.");

        // Merge B into A.
        a.Merge(b);

        // A should now be at full capacity.
        Assert(!a.TryAcquire(), "Merge: after merging, A should reject further tokens.");

        // After window expires, both should accept again.
        Thread.Sleep(1100);
        Assert(a.TryAcquire(), "Merge: after expiry, A should accept token.");
        Assert(b.TryAcquire(), "Merge: after expiry, B should accept token.");
    }

    static void TestConcurrencySafety()
    {
        const int limit = 1000;
        var limiter = new SlidingWindowRateLimiter(limit, TimeSpan.FromSeconds(2));
        int successCount = 0;
        int threadCount = 8;
        var threads = new List<Thread>();

        for (int t = 0; t < threadCount; t++)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    if (limiter.TryAcquire())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        break;
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        Assert(successCount == limit, $"Concurrency: expected {limit} successful acquisitions, got {successCount}.");
    }
}
