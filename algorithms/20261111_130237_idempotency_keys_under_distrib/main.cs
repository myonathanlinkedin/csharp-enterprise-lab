using System;
using System.Threading.Tasks;

namespace IdempotencyDemo
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        }

        private static void RunAllTests()
        {
            TestBasicAddAndRetrieve();
            TestDuplicateDetection();
            TestExpirationBehaviour();
            TestScopeIsolation();
            TestConcurrentAdditions();
            Console.WriteLine("All idempotency store tests passed.");
        }

        private static void TestBasicAddAndRetrieve()
        {
            var store = new InMemoryIdempotencyStore();
            var ttl = TimeSpan.FromSeconds(5);
            bool alreadyExists;

            var added = store.TryAdd("clientA", "op123", "ResultA", ttl, out alreadyExists);
            Assert(added, "First insertion should succeed.");
            Assert(!alreadyExists, "First insertion must not report existing.");

            var got = store.TryGet("clientA", "op123", out var result);
            Assert(got, "Should retrieve stored result.");
            Assert((string)result == "ResultA", "Retrieved result must match stored value.");
        }

        private static void TestDuplicateDetection()
        {
            var store = new InMemoryIdempotencyStore();
            var ttl = TimeSpan.FromSeconds(5);
            bool alreadyExists;

            store.TryAdd("clientB", "opXYZ", "First", ttl, out alreadyExists);
            Assert(!alreadyExists, "Initial add must not be duplicate.");

            var second = store.TryAdd("clientB", "opXYZ", "Second", ttl, out alreadyExists);
            Assert(!second, "Second add should be rejected due to duplicate.");
            Assert(alreadyExists, "Duplicate flag must be true.");

            var got = store.TryGet("clientB", "opXYZ", out var result);
            Assert(got, "Result should still be retrievable.");
            Assert((string)result == "First", "Stored result must remain the first value.");
        }

        private static void TestExpirationBehaviour()
        {
            var store = new InMemoryIdempotencyStore();
            var shortTtl = TimeSpan.FromMilliseconds(200);
            bool alreadyExists;

            store.TryAdd("clientC", "tempKey", "TempResult", shortTtl, out alreadyExists);
            Assert(!alreadyExists, "Initial add must succeed.");

            // Wait for expiration.
            Task.Delay(300).Wait();

            // Cleanup should purge the expired entry.
            store.CleanupExpired();

            var exists = store.TryGet("clientC", "tempKey", out var _);
            Assert(!exists, "Expired entry must not be retrievable.");

            // Re‑add after expiration.
            var addedAgain = store.TryAdd("clientC", "tempKey", "NewResult", shortTtl, out alreadyExists);
            Assert(addedAgain, "Re‑addition after expiration must succeed.");
            Assert(!alreadyExists, "Re‑addition should not be flagged as duplicate.");
        }

        private static void TestScopeIsolation()
        {
            var store = new InMemoryIdempotencyStore();
            var ttl = TimeSpan.FromSeconds(5);
            bool alreadyExists;

            store.TryAdd("scope1", "sharedKey", "Result1", ttl, out alreadyExists);
            Assert(!alreadyExists, "First scoped insert must succeed.");

            store.TryAdd("scope2", "sharedKey", "Result2", ttl, out alreadyExists);
            Assert(!alreadyExists, "Different scope must allow same logical key.");

            var got1 = store.TryGet("scope1", "sharedKey", out var res1);
            var got2 = store.TryGet("scope2", "sharedKey", out var res2);
            Assert(got1 && got2, "Both scoped entries must be present.");
            Assert((string)res1 == "Result1", "Scope1 result mismatch.");
            Assert((string)res2 == "Result2", "Scope2 result mismatch.");
        }

        private static void TestConcurrentAdditions()
        {
            var store = new InMemoryIdempotencyStore();
            var ttl = TimeSpan.FromSeconds(5);
            const int parallelism = 16;
            var successCount = 0;
            var duplicateCount = 0;

            Parallel.For(0, parallelism, i =>
            {
                bool alreadyExists;
                var added = store.TryAdd("concScope", "concKey", $"Value{i}", ttl, out alreadyExists);
                if (added) System.Threading.Interlocked.Increment(ref successCount);
                if (alreadyExists) System.Threading.Interlocked.Increment(ref duplicateCount);
            });

            Assert(successCount == 1, $"Exactly one thread should succeed, but got {successCount}.");
            Assert(duplicateCount == parallelism - 1, $"Remaining threads must see duplicate, got {duplicateCount}.");

            var got = store.TryGet("concScope", "concKey", out var stored);
            Assert(got, "Stored value must be retrievable after concurrent adds.");
            // The stored value is nondeterministic which thread won; just ensure it's one of the expected formats.
            Assert(stored is string s && s.StartsWith("Value"), "Stored value format invalid.");
        }

        private static void Main()
        {
            RunAllTests();
        }
    }
}
