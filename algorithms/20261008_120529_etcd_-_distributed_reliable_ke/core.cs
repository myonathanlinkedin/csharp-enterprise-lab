using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EtcdDemo
{
    internal sealed class Entry
    {
        public string Value { get; set; }
        public long Revision { get; set; }
        public long? LeaseId { get; set; }
    }

    internal sealed class LeaseInfo
    {
        public DateTime Expiration { get; set; }
        public HashSet<string> Keys { get; } = new HashSet<string>();
    }

    public sealed class EtcdStore : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Entry> _kv = new Dictionary<string, Entry>();
        private readonly Dictionary<long, LeaseInfo> _leases = new Dictionary<long, LeaseInfo>();
        private readonly Dictionary<string, List<Action<string, string>>> _watchers = new Dictionary<string, List<Action<string, string>>>();
        private long _revision;
        private long _nextLeaseId = 1;
        private readonly Timer _leaseTimer;
        private bool _disposed;

        public EtcdStore()
        {
            // Check leases every 200 ms
            _leaseTimer = new Timer(LeaseTimerCallback, null, 200, 200);
        }

        private void LeaseTimerCallback(object state)
        {
            List<long> expired = null;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _leases)
                {
                    if (kvp.Value.Expiration <= now)
                    {
                        if (expired == null) expired = new List<long>();
                        expired.Add(kvp.Key);
                    }
                }

                if (expired != null)
                {
                    foreach (var leaseId in expired)
                    {
                        if (_leases.TryGetValue(leaseId, out var lease))
                        {
                            foreach (var key in lease.Keys)
                            {
                                if (_kv.Remove(key))
                                    NotifyWatchers(key, null);
                            }
                            _leases.Remove(leaseId);
                        }
                    }
                }
            }
        }

        private void NotifyWatchers(string key, string newValue)
        {
            if (_watchers.TryGetValue(key, out var list))
            {
                // Copy to avoid modification during iteration
                var callbacks = list.ToArray();
                foreach (var cb in callbacks)
                {
                    try { cb(key, newValue); } catch { /* swallow */ }
                }
            }
        }

        public long Put(string key, string value, long? leaseId = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_lock)
            {
                _revision++;
                var entry = new Entry { Value = value, Revision = _revision, LeaseId = leaseId };
                _kv[key] = entry;

                if (leaseId.HasValue)
                {
                    if (!_leases.TryGetValue(leaseId.Value, out var lease))
                        throw new InvalidOperationException("Lease does not exist.");
                    lease.Keys.Add(key);
                }

                NotifyWatchers(key, value);
                return _revision;
            }
        }

        public (string Value, long Revision)? Get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            lock (_lock)
            {
                if (_kv.TryGetValue(key, out var entry))
                    return (entry.Value, entry.Revision);
                return null;
            }
        }

        public bool Delete(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            lock (_lock)
            {
                if (_kv.Remove(key, out var removed))
                {
                    _revision++;
                    if (removed.LeaseId.HasValue && _leases.TryGetValue(removed.LeaseId.Value, out var lease))
                        lease.Keys.Remove(key);
                    NotifyWatchers(key, null);
                    return true;
                }
                return false;
            }
        }

        public bool CompareAndSwap(string key, string expectedValue, string newValue)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (expectedValue == null) throw new ArgumentNullException(nameof(expectedValue));
            if (newValue == null) throw new ArgumentNullException(nameof(newValue));

            lock (_lock)
            {
                if (_kv.TryGetValue(key, out var entry) && entry.Value == expectedValue)
                {
                    _revision++;
                    entry.Value = newValue;
                    entry.Revision = _revision;
                    NotifyWatchers(key, newValue);
                    return true;
                }
                return false;
            }
        }

        public long CreateLease(TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
            lock (_lock)
            {
                var leaseId = _nextLeaseId++;
                var lease = new LeaseInfo { Expiration = DateTime.UtcNow.Add(ttl) };
                _leases[leaseId] = lease;
                return leaseId;
            }
        }

        public void KeepAliveLease(long leaseId, TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
            lock (_lock)
            {
                if (!_leases.TryGetValue(leaseId, out var lease))
                    throw new InvalidOperationException("Lease not found.");
                lease.Expiration = DateTime.UtcNow.Add(ttl);
            }
        }

        public IDisposable Watch(string key, Action<string, string> callback)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                if (!_watchers.TryGetValue(key, out var list))
                {
                    list = new List<Action<string, string>>();
                    _watchers[key] = list;
                }
                list.Add(callback);
            }

            return new Unsubscriber(this, key, callback);
        }

        private void Unwatch(string key, Action<string, string> callback)
        {
            lock (_lock)
            {
                if (_watchers.TryGetValue(key, out var list))
                {
                    list.Remove(callback);
                    if (list.Count == 0)
                        _watchers.Remove(key);
                }
            }
        }

        private sealed class Unsubscriber : IDisposable
        {
            private EtcdStore _store;
            private readonly string _key;
            private readonly Action<string, string> _callback;
            public Unsubscriber(EtcdStore store, string key, Action<string, string> callback)
            {
                _store = store;
                _key = key;
                _callback = callback;
            }
            public void Dispose()
            {
                var s = Interlocked.Exchange(ref _store, null);
                if (s != null)
                    s.Unwatch(_key, _callback);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _leaseTimer.Dispose();
            _disposed = true;
        }
    }
}
