using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

namespace Idempotency
{
    /// <summary>
    /// Thread‑safe in‑memory store for idempotency keys with scoped isolation and TTL expiration.
    /// </summary>
    public sealed class IdempotencyKeyStore
    {
        private sealed class Entry
        {
            public object Value { get; }
            public DateTime ExpirationUtc { get; }

            public Entry(object value, TimeSpan ttl)
            {
                Value = value;
                ExpirationUtc = DateTime.UtcNow.Add(ttl);
            }

            public bool IsExpired => DateTime.UtcNow >= ExpirationUtc;
        }

        // Scope → (Key → Entry)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Entry>> _store
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, Entry>>(StringComparer.Ordinal);

        /// <summary>
        /// Attempts to add a new idempotency key within a given scope.
        /// </summary>
        /// <param name="scope">Logical grouping (e.g., user id, service name).</param>
        /// <param name="key">Idempotency key supplied by the client.</param>
        /// <param name="value">Arbitrary payload to associate with the key.</param>
        /// <param name="ttl">Time‑to‑live after which the key expires.</param>
        /// <param name="existingValue">If the key already exists and is not expired, its stored value.</param>
        /// <returns>True if the key was newly added; false if it already existed and is still valid.</returns>
        public bool TryAdd(string scope, string key, object value, TimeSpan ttl, out object existingValue)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));

            var scopedDict = _store.GetOrAdd(scope, _ => new ConcurrentDictionary<string, Entry>(StringComparer.Ordinal));

            while (true)
            {
                // Try to add a fresh entry.
                var newEntry = new Entry(value, ttl);
                if (scopedDict.TryAdd(key, newEntry))
                {
                    existingValue = null;
                    return true;
                }

                // Key already present – inspect the existing entry.
                if (scopedDict.TryGetValue(key, out var existingEntry))
                {
                    if (existingEntry.IsExpired)
                    {
                        // Attempt to replace the expired entry.
                        if (scopedDict.TryUpdate(key, newEntry, existingEntry))
                        {
                            existingValue = null;
                            return true;
                        }
                        // If update failed, loop again – another thread may have intervened.
                        continue;
                    }
                    else
                    {
                        existingValue = existingEntry.Value;
                        return false;
                    }
                }

                // Rare race: key disappeared after TryAdd failed. Loop to retry.
            }
        }

        /// <summary>
        /// Retrieves the stored value for a given scoped key if it exists and is not expired.
        /// </summary>
        public bool TryGet(string scope, string key, out object value)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (key == null) throw new ArgumentNullException(nameof(key));

            value = null;
            if (_store.TryGetValue(scope, out var scopedDict) &&
                scopedDict.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    // Lazy cleanup of the stale entry.
                    scopedDict.TryRemove(key, out _);
                    return false;
                }

                value = entry.Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all expired entries across all scopes. Intended for periodic maintenance.
        /// </summary>
        public void RemoveExpired()
        {
            foreach (var kvp in _store)
            {
                var scopedDict = kvp.Value;
                foreach (var keyEntry in scopedDict)
                {
                    if (keyEntry.Value.IsExpired)
                    {
                        scopedDict.TryRemove(keyEntry.Key, out _);
                    }
                }

                // If a scope becomes empty, prune it.
                if (scopedDict.IsEmpty)
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
