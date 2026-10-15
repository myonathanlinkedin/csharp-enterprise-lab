using System;
using System.Collections.Generic;

namespace DistributedScheduler
{
    // Simple clock abstraction for deterministic testing
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    // Job status enumeration
    public enum JobStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Dead
    }

    // Represents a unit of work
    public sealed class Job
    {
        public Guid Id { get; }
        public string Payload { get; }
        public int MaxRetries { get; }
        public int RetryCount { get; private set; }
        public JobStatus Status { get; private set; }

        public Job(string payload, int maxRetries = 3)
        {
            Id = Guid.NewGuid();
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            MaxRetries = maxRetries >= 0 ? maxRetries : throw new ArgumentOutOfRangeException(nameof(maxRetries));
            RetryCount = 0;
            Status = JobStatus.Pending;
        }

        internal void MarkRunning()
        {
            if (Status != JobStatus.Pending && Status != JobStatus.Failed)
                throw new InvalidOperationException("Job cannot transition to Running from its current state.");
            Status = JobStatus.Running;
        }

        internal void MarkCompleted()
        {
            if (Status != JobStatus.Running)
                throw new InvalidOperationException("Only a running job can be completed.");
            Status = JobStatus.Completed;
        }

        internal void MarkFailed()
        {
            if (Status != JobStatus.Running)
                throw new InvalidOperationException("Only a running job can fail.");
            RetryCount++;
            Status = RetryCount > MaxRetries ? JobStatus.Dead : JobStatus.Failed;
        }

        internal void ResetToPending()
        {
            if (Status != JobStatus.Failed)
                throw new InvalidOperationException("Only a failed job can be reset.");
            Status = JobStatus.Pending;
        }
    }

    // Lease representing a worker's claim on a job
    public sealed class Lease
    {
        public Guid LeaseId { get; }
        public Guid JobId { get; }
        public string WorkerId { get; }
        public DateTime ExpirationUtc { get; private set; }

        public Lease(Guid jobId, string workerId, TimeSpan leaseDuration, IClock clock)
        {
            LeaseId = Guid.NewGuid();
            JobId = jobId;
            WorkerId = workerId ?? throw new ArgumentNullException(nameof(workerId));
            ExpirationUtc = clock.UtcNow.Add(leaseDuration);
        }

        public void Extend(TimeSpan additional, IClock clock)
        {
            if (additional <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(additional));
            ExpirationUtc = clock.UtcNow.Add(additional);
        }

        public bool IsExpired(IClock clock) => clock.UtcNow >= ExpirationUtc;
    }

    // Core scheduler handling jobs and leases
    public sealed class Scheduler
    {
        private readonly IClock _clock;
        private readonly Dictionary<Guid, Job> _jobs = new();
        private readonly Queue<Guid> _pendingQueue = new();
        private readonly Dictionary<Guid, Lease> _activeLeases = new();

        public Scheduler(IClock? clock = null)
        {
            _clock = clock ?? new SystemClock();
        }

        // Enqueue a new job
        public void EnqueueJob(Job job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (_jobs.ContainsKey(job.Id))
                throw new InvalidOperationException("Job with same Id already exists.");
            _jobs[job.Id] = job;
            _pendingQueue.Enqueue(job.Id);
        }

        // Try to acquire a lease for the next pending job
        public Lease? TryAcquireJob(string workerId, TimeSpan leaseDuration)
        {
            if (workerId == null) throw new ArgumentNullException(nameof(workerId));
            if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));

            CleanupExpiredLeases();

            while (_pendingQueue.Count > 0)
            {
                var jobId = _pendingQueue.Dequeue();
                if (!_jobs.TryGetValue(jobId, out var job))
                    continue; // Should not happen, but guard against corruption

                if (job.Status != JobStatus.Pending && job.Status != JobStatus.Failed)
                    continue; // Skip non-runnable jobs

                job.MarkRunning();
                var lease = new Lease(jobId, workerId, leaseDuration, _clock);
                _activeLeases[lease.LeaseId] = lease;
                return lease;
            }

            return null; // No pending jobs
        }

        // Complete a job successfully
        public void CompleteJob(Guid leaseId)
        {
            if (!_activeLeases.TryGetValue(leaseId, out var lease))
                throw new InvalidOperationException("Lease not found or already released.");

            var job = GetJobOrThrow(lease.JobId);
            job.MarkCompleted();
            _activeLeases.Remove(leaseId);
        }

        // Fail a job; may be retried or marked dead
        public void FailJob(Guid leaseId)
        {
            if (!_activeLeases.TryGetValue(leaseId, out var lease))
                throw new InvalidOperationException("Lease not found or already released.");

            var job = GetJobOrThrow(lease.JobId);
            job.MarkFailed();
            _activeLeases.Remove(leaseId);

            if (job.Status == JobStatus.Failed)
            {
                job.ResetToPending();
                _pendingQueue.Enqueue(job.Id);
            }
            // If Dead, job stays out of the queue.
        }

        // Extend an existing lease
        public void ExtendLease(Guid leaseId, TimeSpan additionalDuration)
        {
            if (!_activeLeases.TryGetValue(leaseId, out var lease))
                throw new InvalidOperationException("Lease not found.");

            lease.Extend(additionalDuration, _clock);
        }

        // Internal helper to fetch job or throw
        private Job GetJobOrThrow(Guid jobId)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
                throw new InvalidOperationException("Associated job not found.");
            return job;
        }

        // Remove leases that have expired and re-queue their jobs
        private void CleanupExpiredLeases()
        {
            var expired = new List<Guid>();
            foreach (var kvp in _activeLeases)
            {
                if (kvp.Value.IsExpired(_clock))
                {
                    expired.Add(kvp.Key);
                }
            }

            foreach (var leaseId in expired)
            {
                var lease = _activeLeases[leaseId];
                var job = GetJobOrThrow(lease.JobId);
                // Only requeue if job is still marked as Running (it may have been completed already)
                if (job.Status == JobStatus.Running)
                {
                    job.MarkFailed(); // Count as a failure due to lease expiry
                    if (job.Status == JobStatus.Failed)
                    {
                        job.ResetToPending();
                        _pendingQueue.Enqueue(job.Id);
                    }
                }
                _activeLeases.Remove(leaseId);
            }
        }

        // Expose read‑only snapshots for testing
        public IReadOnlyCollection<Job> AllJobs => _jobs.Values;
        public IReadOnlyCollection<Lease> ActiveLeases => _activeLeases.Values;
        public int PendingCount => _pendingQueue.Count;
    }
}
