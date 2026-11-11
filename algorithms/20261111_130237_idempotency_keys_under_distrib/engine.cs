using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

namespace IdempotencyDemo
{
    /// <summary>
    /// Thread‑safe in‑memory implementation of <see cref="IIdempotencyStore"/>.
    /// </summary>
    public sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        // Composite key: $"{scope}:{key}"
        private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();

        private static string ComposeKey(string scope, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            // Normalise null/empty scope to a constant token.
            var normScope = string.IsNullOrEmpty(scope) ? "<default>" : scope;
            return $"{normScope}:{key}";
        }

        public bool TryAdd(string scope, string key, object result, TimeSpan ttl, out bool alreadyExists)
        {
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
            var composite = ComposeKey(scope, key);
            while (true)
            {
                // Try to get an existing record.
                if (_store.TryGetValue(composite, out var existing))
                {
                    if (!existing.IsExpired)
                    {
                        alreadyExists = true;
                        return false; // Existing valid entry – do not overwrite.
                    }
                    // Existing entry is expired – attempt to replace.
                    var newRecord = new IdempotencyRecord(result, ttl);
                    if (_store.TryUpdate(composite, newRecord, existing))
                    {
                        alreadyExists = false;
                        return true;
                    }
                    // Race condition – retry loop.
                    continue;
                }

                // No entry present – attempt to add.
                var record = new IdempotencyRecord(result, ttl);
                if (_store.TryAdd(composite, record))
                {
                    alreadyExists = false;
                    return true;
                }
                // Another thread added concurrently – retry.
            }
        }

        public bool TryGet(string scope, string key, out object result)
        {
            var composite = ComposeKey(scope, key);
            if (_store.TryGetValue(composite, out var record))
            {
                if (!record.IsExpired)
                {
                    result = record.Result;
                    return true;
                }
                // Record expired – remove it.
                _store.TryRemove(composite, out _);
            }
            result = null;
            return false;
        }

        public void CleanupExpired()
        {
            foreach (var kvp in _store)
            {
                if (kvp.Value.IsExpired)
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
