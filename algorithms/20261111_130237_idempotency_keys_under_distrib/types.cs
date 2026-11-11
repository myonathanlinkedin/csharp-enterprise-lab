using System;

namespace IdempotencyDemo
{
    /// <summary>
    /// Represents a stored idempotency record.
    /// </summary>
    public sealed class IdempotencyRecord
    {
        public object Result { get; }
        public DateTime ExpirationUtc { get; }

        public IdempotencyRecord(object result, TimeSpan ttl)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            ExpirationUtc = DateTime.UtcNow.Add(ttl);
        }

        public bool IsExpired => DateTime.UtcNow >= ExpirationUtc;
    }

    /// <summary>
    /// Contract for an idempotency key store.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// Attempts to add a new record. If a non‑expired record already exists,
        /// the method returns false and sets <paramref name="alreadyExists"/> to true.
        /// </summary>
        bool TryAdd(string scope, string key, object result, TimeSpan ttl, out bool alreadyExists);

        /// <summary>
        /// Tries to retrieve a stored result for the given scope/key.
        /// Returns true if a non‑expired record exists.
        /// </summary>
        bool TryGet(string scope, string key, out object result);

        /// <summary>
        /// Removes all expired entries from the store.
        /// </summary>
        void CleanupExpired();
    }
}
