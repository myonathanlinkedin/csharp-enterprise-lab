using System;
using System.Security.Cryptography;
using System.Text;

namespace CuckooFilterDemo
{
    /// <summary>
    /// Production‑grade cuckoo filter implementation with deterministic behaviour.
    /// </summary>
    public sealed class CuckooFilter : ICuckooFilter
    {
        private readonly CuckooFilterConfig _config;
        private readonly ushort[][] _buckets; // each entry stores a fingerprint (0 = empty)
        private readonly Random _rand; // deterministic seed for reproducibility

        // Constants for index mixing
        private const uint IndexHashMultiplier = 0x5bd1e995u;

        public CuckooFilter(CuckooFilterConfig config, int deterministicSeed = 42)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _buckets = new ushort[_config.BucketCount][];
            for (int i = 0; i < _buckets.Length; i++)
                _buckets[i] = new ushort[_config.BucketSize];
            _rand = new Random(deterministicSeed);
        }

        public bool Insert(string item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var (fp, i1, i2) = ComputeIndices(item);
            // Try to place directly
            if (TryAddToBucket(i1, fp) || TryAddToBucket(i2, fp))
                return true;

            // Eviction loop
            int curIdx = _rand.Next(2) == 0 ? i1 : i2;
            ushort curFp = fp;

            for (int kick = 0; kick < _config.MaxKicks; kick++)
            {
                // Randomly select a slot in the current bucket to evict
                int slot = _rand.Next(_config.BucketSize);
                ushort evictedFp = _buckets[curIdx][slot];
                _buckets[curIdx][slot] = curFp;
                curFp = evictedFp;

                // Compute alternate index for evicted fingerprint
                curIdx = AlternateIndex(curIdx, curFp);
                if (TryAddToBucket(curIdx, curFp))
                    return true;
            }

            // Insertion failed after max kicks
            return false;
        }

        public bool Contains(string item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var (fp, i1, i2) = ComputeIndices(item);
            return BucketContains(i1, fp) || BucketContains(i2, fp);
        }

        public bool Delete(string item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var (fp, i1, i2) = ComputeIndices(item);
            if (RemoveFromBucket(i1, fp))
                return true;
            if (RemoveFromBucket(i2, fp))
                return true;
            return false;
        }

        // ---------- Helper methods ----------
        private (ushort fp, int i1, int i2) ComputeIndices(string item)
        {
            // 64‑bit hash using SHA256 (first 8 bytes)
            ulong hash = ComputeHash64(item);
            ushort fp = FingerprintFromHash(hash);
            // Ensure fingerprint is never zero (zero denotes empty slot)
            if (fp == 0) fp = 1;

            int i1 = (int)(hash % (ulong)_config.BucketCount);
            int i2 = AlternateIndex(i1, fp);
            return (fp, i1, i2);
        }

        private static ulong ComputeHash64(string item)
        {
            using var sha = SHA256.Create();
            byte[] data = Encoding.UTF8.GetBytes(item);
            byte[] hash = sha.ComputeHash(data);
            // Take first 8 bytes as little‑endian ulong
            return BitConverter.ToUInt64(hash, 0);
        }

        private ushort FingerprintFromHash(ulong hash)
        {
            // Extract the low FingerprintSize bits
            int bits = _config.FingerprintSize;
            ushort mask = (ushort)((1 << bits) - 1);
            return (ushort)(hash & mask);
        }

        private int AlternateIndex(int index, ushort fingerprint)
        {
            uint fpHash = ((uint)fingerprint) * IndexHashMultiplier;
            int alt = (int)((index ^ fpHash) & ((uint)_config.BucketCount - 1));
            return alt;
        }

        private bool TryAddToBucket(int bucketIdx, ushort fp)
        {
            ushort[] bucket = _buckets[bucketIdx];
            for (int i = 0; i < bucket.Length; i++)
            {
                if (bucket[i] == 0)
                {
                    bucket[i] = fp;
                    return true;
                }
            }
            return false;
        }

        private bool BucketContains(int bucketIdx, ushort fp)
        {
            ushort[] bucket = _buckets[bucketIdx];
            for (int i = 0; i < bucket.Length; i++)
                if (bucket[i] == fp)
                    return true;
            return false;
        }

        private bool RemoveFromBucket(int bucketIdx, ushort fp)
        {
            ushort[] bucket = _buckets[bucketIdx];
            for (int i = 0; i < bucket.Length; i++)
            {
                if (bucket[i] == fp)
                {
                    bucket[i] = 0;
                    return true;
                }
            }
            return false;
        }
    }
}
