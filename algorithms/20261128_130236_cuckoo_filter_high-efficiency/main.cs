using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CuckooFilterDemo
{
    internal static class Program
    {
        private static void Main()
        {
            // Basic functional tests
            TestInsertionAndLookup();
            TestDeletion();
            TestFalsePositiveRate();

            Console.WriteLine("All tests passed.");
        }

        private static void TestInsertionAndLookup()
        {
            var config = new CuckooFilterConfig(bucketCount: 1 << 12, bucketSize: 4, fingerprintSize: 8);
            var filter = new CuckooFilter(config, deterministicSeed: 123);

            const int itemCount = 5000;
            var items = new List<string>(itemCount);
            for (int i = 0; i < itemCount; i++)
                items.Add($"item-{i}");

            // Insert all items
            foreach (var s in items)
                Debug.Assert(filter.Insert(s), $"Failed to insert {s}");

            // Verify presence
            foreach (var s in items)
                Debug.Assert(filter.Contains(s), $"Missing {s}");
        }

        private static void TestDeletion()
        {
            var config = new CuckooFilterConfig(bucketCount: 1 << 10, bucketSize: 4, fingerprintSize: 8);
            var filter = new CuckooFilter(config, deterministicSeed: 456);

            var toInsert = new[] { "alpha", "beta", "gamma", "delta" };
            foreach (var s in toInsert)
                Debug.Assert(filter.Insert(s), $"Insert failed for {s}");

            // Delete a subset
            Debug.Assert(filter.Delete("beta"), "Delete failed for beta");
            Debug.Assert(!filter.Contains("beta"), "beta should not be present after deletion");

            // Ensure others still present
            Debug.Assert(filter.Contains("alpha"));
            Debug.Assert(filter.Contains("gamma"));
            Debug.Assert(filter.Contains("delta"));
        }

        private static void TestFalsePositiveRate()
        {
            var config = new CuckooFilterConfig(bucketCount: 1 << 11, bucketSize: 4, fingerprintSize: 8);
            var filter = new CuckooFilter(config, deterministicSeed: 789);

            const int inserted = 2000;
            const int queries = 2000;
            var insertedSet = new HashSet<string>();
            for (int i = 0; i < inserted; i++)
            {
                string val = $"key-{i}";
                insertedSet.Add(val);
                Debug.Assert(filter.Insert(val));
            }

            int falsePositives = 0;
            for (int i = inserted; i < inserted + queries; i++)
            {
                string val = $"key-{i}";
                if (filter.Contains(val))
                    falsePositives++;
            }

            double fpRate = (double)falsePositives / queries;
            // With 8‑bit fingerprint and 4‑way buckets, theoretical fp ≤ ~0.02.
            // We allow a small margin.
            Debug.Assert(fpRate <= 0.05, $"False positive rate too high: {fpRate:P2}");
        }
    }
}
