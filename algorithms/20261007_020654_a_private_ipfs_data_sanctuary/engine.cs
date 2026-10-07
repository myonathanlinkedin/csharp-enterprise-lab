using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PrivateIpfsSanctuary
{
    // Core implementation of a private IPFS‑like sanctuary.
    public sealed class Sanctuary : IContentAddressableStorage
    {
        // Thread‑safe storage mapping hash -> DigitalObject.
        private readonly ConcurrentDictionary<string, DigitalObject> _store = new();

        // Adds data to the sanctuary, computing its SHA‑256 hash.
        public DigitalObject Add(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string hash = ComputeHash(data);
            // Ensure deterministic GUID for the same content.
            Guid id = Guid.NewGuid();

            var obj = new DigitalObject(id, (byte[])data.Clone(), hash);
            // Store only if not already present.
            _store.TryAdd(hash, obj);
            return obj;
        }

        // Retrieves a stored object by hash.
        public DigitalObject Get(string hash)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));

            if (_store.TryGetValue(hash, out var obj))
                return obj;

            throw new InvalidOperationException($"Object with hash '{hash}' not found.");
        }

        // Verifies integrity of a stored object.
        public bool Verify(string hash)
        {
            var obj = Get(hash);
            string recomputed = ComputeHash(obj.Data);
            return string.Equals(recomputed, obj.Hash, StringComparison.OrdinalIgnoreCase);
        }

        // Computes SHA‑256 hash and returns as hex string.
        private static string ComputeHash(byte[] data)
        {
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(data);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
