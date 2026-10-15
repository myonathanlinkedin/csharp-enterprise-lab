using System;
using System.Collections.Generic;
using DistributedScheduler;

namespace DistributedSchedulerTest
{
    // Simple deterministic clock for unit tests
    internal sealed class FakeClock : IClock
    {
        private DateTime _now;
        public FakeClock(DateTime start) => _now = start;
        public DateTime UtcNow => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }

    internal static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition) throw new InvalidOperationException("Assert.IsTrue failed. " + message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Assert.AreEqual failed. Expected:{expected} Actual:{actual}. {message}");
        }

        public static void IsNull(object? obj, string message = "")
        {
            if (obj != null) throw new InvalidOperationException("Assert.IsNull failed. " + message);
        }

        public static void IsNotNull(object? obj, string message = "")
        {
            if (obj == null) throw new InvalidOperationException("Assert.IsNotNull failed. " + message);
        }
    }

    internal static class SchedulerTests
    {
        public static void RunAll()
        {
            TestEnqueueAndAcquire();
            TestCompleteJob();
            TestFailAndRetry();
            TestLeaseExpirationRequeues();
            TestMaxRetryLeadsToDead();
            Console.WriteLine("All Scheduler tests passed.");
        }

        private static void TestEnqueueAndAcquire()
        {
            var clock = new FakeClock(DateTime.UtcNow);
            var scheduler = new Scheduler(clock);
            var job = new Job("payload1");
            scheduler.EnqueueJob(job);

            Assert.AreEqual(1, scheduler.PendingCount);
            var lease = scheduler.TryAcquireJob("workerA", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease);
            Assert.AreEqual(JobStatus.Running, job.Status);
            Assert.AreEqual(0, scheduler.PendingCount);
        }

        private static void TestCompleteJob()
        {
            var clock = new FakeClock(DateTime.UtcNow);
            var scheduler = new Scheduler(clock);
            var job = new Job("payload2");
            scheduler.EnqueueJob(job);
            var lease = scheduler.TryAcquireJob("workerB", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease);
            scheduler.CompleteJob(lease!.LeaseId);
            Assert.AreEqual(JobStatus.Completed, job.Status);
            Assert.AreEqual(0, scheduler.ActiveLeases.Count);
        }

        private static void TestFailAndRetry()
        {
            var clock = new FakeClock(DateTime.UtcNow);
            var scheduler = new Scheduler(clock);
            var job = new Job("payload3", maxRetries: 2);
            scheduler.EnqueueJob(job);
            var lease = scheduler.TryAcquireJob("workerC", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease);
            scheduler.FailJob(lease!.LeaseId);
            Assert.AreEqual(JobStatus.Pending, job.Status);
            Assert.AreEqual(1, job.RetryCount);
            Assert.AreEqual(1, scheduler.PendingCount);
        }

        private static void TestLeaseExpirationRequeues()
        {
            var clock = new FakeClock(DateTime.UtcNow);
            var scheduler = new Scheduler(clock);
            var job = new Job("payload4", maxRetries: 1);
            scheduler.EnqueueJob(job);
            var lease = scheduler.TryAcquireJob("workerD", TimeSpan.FromSeconds(10));
            Assert.IsNotNull(lease);
            // Advance time beyond lease expiration
            clock.Advance(TimeSpan.FromSeconds(11));
            // Next acquisition triggers cleanup
            var lease2 = scheduler.TryAcquireJob("workerE", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease2);
            // The job should have been marked as failed once due to expiration
            Assert.AreEqual(1, job.RetryCount);
            Assert.AreEqual(JobStatus.Pending, job.Status);
        }

        private static void TestMaxRetryLeadsToDead()
        {
            var clock = new FakeClock(DateTime.UtcNow);
            var scheduler = new Scheduler(clock);
            var job = new Job("payload5", maxRetries: 1);
            scheduler.EnqueueJob(job);
            // First attempt fails
            var lease1 = scheduler.TryAcquireJob("workerF", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease1);
            scheduler.FailJob(lease1!.LeaseId);
            Assert.AreEqual(JobStatus.Pending, job.Status);
            // Second attempt fails again, exceeding max retries
            var lease2 = scheduler.TryAcquireJob("workerG", TimeSpan.FromMinutes(5));
            Assert.IsNotNull(lease2);
            scheduler.FailJob(lease2!.LeaseId);
            Assert.AreEqual(JobStatus.Dead, job.Status);
            Assert.AreEqual(0, scheduler.PendingCount);
        }
    }

    internal class Program
    {
        private static void Main()
        {
            SchedulerTests.RunAll();
        }
    }
}
