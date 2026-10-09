using System;
using System.Security.Cryptography;
using System.Text;

namespace HyperLogLogNS
{
    internal static class HashHelper
    {
        public static ulong ComputeHash64(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] hash = sha256.ComputeHash(bytes);
            // Take the first 8 bytes (little‑endian) as a 64‑bit hash value
            return BitConverter.ToUInt64(hash, 0);
        }
    }
}
