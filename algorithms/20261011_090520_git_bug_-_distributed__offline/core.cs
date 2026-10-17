using System;
using System.Collections.Generic;
using System.Linq;

namespace GitBugTracker
{
    public enum BugStatus
    {
        Open,
        InProgress,
        Resolved,
        Closed
    }

    public sealed class Bug
    {
        public Guid Id { get; }
        public string Title { get; }
        public string Description { get; }
        public BugStatus Status { get; }

        public Bug(Guid id, string title, string description, BugStatus status)
        {
            Id = id;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Status = status;
        }

        public Bug With(
            string? title = null,
            string? description = null,
            BugStatus? status = null)
        {
            return new Bug(
                Id,
                title ?? Title,
                description ?? Description,
                status ?? Status);
        }
    }

    internal sealed class BugRecord
    {
        public Bug Bug { get; }
        public Dictionary<string, int> VersionVector { get; }
        public DateTime LastModifiedUtc { get; }

        public BugRecord(Bug bug, Dictionary<string, int> versionVector, DateTime lastModifiedUtc)
        {
            Bug = bug ?? throw new ArgumentNullException(nameof(bug));
            VersionVector = new Dictionary<string, int>(versionVector);
            LastModifiedUtc = lastModifiedUtc;
        }

        public BugRecord Clone()
        {
            return new BugRecord(Bug, new Dictionary<string, int>(VersionVector), LastModifiedUtc);
        }
    }

    public sealed class BugTracker
    {
        private readonly string _replicaId;
        private readonly Dictionary<Guid, BugRecord> _store = new();

        public BugTracker(string replicaId)
        {
            _replicaId = replicaId ?? throw new ArgumentNullException(nameof(replicaId));
        }

        // Add a new bug. Returns the generated Bug Id.
        public Guid AddBug(string title, string description, BugStatus status = BugStatus.Open)
        {
            var id = Guid.NewGuid();
            var bug = new Bug(id, title, description, status);
            var vv = new Dictionary<string, int> { [_replicaId] = 1 };
            var record = new BugRecord(bug, vv, DateTime.UtcNow);
            _store[id] = record;
            return id;
        }

        // Update an existing bug. Returns false if bug not found.
        public bool UpdateBug(Guid id, string? title = null, string? description = null, BugStatus? status = null)
        {
            if (!_store.TryGetValue(id, out var existing))
                return false;

            var newBug = existing.Bug.With(title, description, status);
            var newVv = new Dictionary<string, int>(existing.VersionVector);
            if (!newVv.ContainsKey(_replicaId))
                newVv[_replicaId] = 0;
            newVv[_replicaId]++;

            var newRecord = new BugRecord(newBug, newVv, DateTime.UtcNow);
            _store[id] = newRecord;
            return true;
        }

        // Retrieve a copy of a bug; null if not present.
        public Bug? GetBug(Guid id)
        {
            return _store.TryGetValue(id, out var rec) ? rec.Bug : null;
        }

        // Return all bugs as a list (snapshot).
        public List<Bug> ListBugs()
        {
            return _store.Values.Select(r => r.Bug).ToList();
        }

        // Merge another replica's state into this one.
        public void Merge(BugTracker other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            foreach (var kvp in other._store)
            {
                var id = kvp.Key;
                var otherRec = kvp.Value;

                if (!_store.TryGetValue(id, out var localRec))
                {
                    // No local copy – adopt other's record.
                    _store[id] = otherRec.Clone();
                    continue;
                }

                // Determine causal relationship.
                var relation = CompareVersionVectors(localRec.VersionVector, otherRec.VersionVector);
                if (relation == CausalRelation.After)
                {
                    // Local dominates – keep local.
                    continue;
                }
                else if (relation == CausalRelation.Before)
                {
                    // Other dominates – replace.
                    _store[id] = otherRec.Clone();
                }
                else // Concurrent
                {
                    // Resolve deterministically: later timestamp wins.
                    if (otherRec.LastModifiedUtc > localRec.LastModifiedUtc)
                        _store[id] = otherRec.Clone();
                }
            }
        }

        private enum CausalRelation { Concurrent, Before, After }

        private static CausalRelation CompareVersionVectors(
            Dictionary<string, int> a,
            Dictionary<string, int> b)
        {
            bool aGreater = false, bGreater = false;
            var keys = new HashSet<string>(a.Keys);
            keys.UnionWith(b.Keys);
            foreach (var key in keys)
            {
                a.TryGetValue(key, out var av);
                b.TryGetValue(key, out var bv);
                if (av < bv) bGreater = true;
                if (av > bv) aGreater = true;
                if (aGreater && bGreater) return CausalRelation.Concurrent;
            }
            if (aGreater && !bGreater) return CausalRelation.After;
            if (bGreater && !aGreater) return CausalRelation.Before;
            return CausalRelation.Concurrent; // identical vectors
        }
    }
}
