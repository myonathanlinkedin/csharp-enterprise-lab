using System;

namespace CuckooFilterDemo
{
    /// <summary>
    /// Configuration parameters for a cuckoo filter.
    /// </summary>
    public sealed class CuckooFilterConfig
    {
        /// <summary>
        /// Number of buckets (must be power of two for efficient modulo).
        /// </summary>
        public int BucketCount { get; }

        /// <summary>
        /// Number of entries per bucket (commonly 4).
        /// </summary>
        public int BucketSize { get; }

        /// <summary>
        /// Size of the fingerprint in bits (8, 12, 16 are typical). Must be between 1 and 16.
        /// </summary>
        public int FingerprintSize { get; }

        /// <summary>
        /// Maximum number of relocation attempts during insertion before giving up.
        /// </summary>
        public int MaxKicks { get; }

        public CuckooFilterConfig(int bucketCount = 1 << 10, int bucketSize = 4, int fingerprintSize = 8, int maxKicks = 500)
        {
            if (bucketCount <= 0 || (bucketCount & (bucketCount - 1)) != 0)
                throw new ArgumentException("BucketCount must be a positive power of two.", nameof(bucketCount));
            if (bucketSize <= 0)
                throw new ArgumentException("BucketSize must be positive.", nameof(bucketSize));
            if (fingerprintSize <= 0 || fingerprintSize > 16)
                throw new ArgumentException("FingerprintSize must be between 1 and 16.", nameof(fingerprintSize));
            if (maxKicks <= 0)
                throw new ArgumentException("MaxKicks must be positive.", nameof(maxKicks));

            BucketCount = bucketCount;
            BucketSize = bucketSize;
            FingerprintSize = fingerprintSize;
            MaxKicks = maxKicks;
        }
    }

    /// <summary>
    /// Public contract for a cuckoo filter.
    /// </summary>
    public interface ICuckooFilter
    {
        /// <summary>
        /// Attempts to insert an item. Returns true on success, false if the filter is full.
        /// </summary>
        bool Insert(string item);

        /// <summary>
        /// Returns true if the item is (probably) present, false if definitely not present.
        /// </summary>
        bool Contains(string item);

        /// <summary>
        /// Attempts to delete an item. Returns true if the item was found and removed.
        /// </summary>
        bool Delete(string item);
    }
}
