using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Idempotency
{
    /// <summary>
    /// Thread‑safe in‑memory manager for idempotent operations.
    /// Stores a payload hash together with the lazily computed response.
    /// </summary>
    /// <typeparam name="TResponse">Type of the operation result.</typeparam>
    public sealed class IdempotencyManager<TResponse>
    {
        private sealed class Entry
        {
            public string PayloadHash { get; }
            public Lazy<TResponse> Response { get; }
            public DateTime Timestamp; // last access time (UTC)

            public Entry(string payloadHash, Lazy<TResponse> response, DateTime timestamp)
            {
                PayloadHash = payloadHash ?? throw new ArgumentNullException(nameof(payloadHash));
                Response = response ?? throw new ArgumentNullException(nameof(response));
                Timestamp = timestamp;
            }
        }

        private readonly ConcurrentDictionary<string, Entry> _store = new();

        /// <summary>
        /// Executes the operation associated with the given idempotency key.
        /// If the key has been seen before with the same payload hash, the stored response is returned.
        /// If the key exists with a different payload hash, an exception is thrown.
        /// </summary>
        /// <param name="key">Client supplied idempotency key (must be unique per logical request).</param>
        /// <param name="payloadHash">Hash of the request payload (e.g., SHA‑256).</param>
        /// <param name="operation">The operation to execute on first encounter.</param>
        /// <returns>The operation result, either freshly computed or cached.</returns>
        public TResponse Execute(string key, string payloadHash, Func<TResponse> operation)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (payloadHash == null) throw new ArgumentNullException(nameof(payloadHash));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var now = DateTime.UtcNow;

            // Try to add a new entry; if another thread added first we fall back to the existing one.
            var newEntry = new Entry(payloadHash, new Lazy<TResponse>(operation, LazyThreadSafetyMode.ExecutionAndPublication), now);
            var entry = _store.GetOrAdd(key, newEntry);

            // If we inserted the entry, compute the response now.
            if (ReferenceEquals(entry, newEntry))
            {
                // Force evaluation so that any exception bubbles up immediately.
                var result = entry.Response.Value;
                entry.Timestamp = now;
                return result;
            }

            // Existing entry – verify payload hash consistency.
            if (entry.PayloadHash != payloadHash)
                throw new InvalidOperationException("Idempotency key conflict: different payload hash.");

            // Update last‑access timestamp and return cached response.
            entry.Timestamp = now;
            return entry.Response.Value;
        }

        /// <summary>
        /// Removes entries that have not been accessed for longer than the supplied TTL.
        /// </summary>
        /// <param name="timeToLive">Maximum age of an entry before it is eligible for removal.</param>
        public void CleanupExpiredEntries(TimeSpan timeToLive)
        {
            if (timeToLive <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeToLive));

            var now = DateTime.UtcNow;
            foreach (var kvp in _store)
            {
                if (now - kvp.Value.Timestamp > timeToLive)
                    _store.TryRemove(kvp.Key, out _);
            }
        }

        /// <summary>
        /// Returns the current number of stored keys (useful for testing).
        /// </summary>
        public int Count => _store.Count;
    }
}
