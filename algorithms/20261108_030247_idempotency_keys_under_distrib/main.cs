using System;
using System.Threading;
using System.Diagnostics;
using Idempotency;

namespace IdempotencyTest
{
    internal static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition) throw new InvalidOperationException("Assert.IsTrue failed. " + message);
        }

        public static void IsFalse(bool condition, string message = "")
        {
            if (condition) throw new InvalidOperationException("Assert.IsFalse failed. " + message);
        }

        public static void AreEqual(object expected, object actual, string message = "")
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException($"Assert.AreEqual failed. Expected:{expected} Actual:{actual}. {message}");
        }

        public static void IsNull(object obj, string message = "")
        {
            if (obj != null) throw new InvalidOperationException("Assert.IsNull failed. " + message);
        }

        public static void IsNotNull(object obj, string message = "")
        {
            if (obj == null) throw new InvalidOperationException("Assert.IsNotNull failed. " + message);
        }
    }

    internal class Program
    {
        private static void TestBasicAddAndGet()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromSeconds(5);
            object existing;

            bool added = store.TryAdd("user-1", "key-123", "payload", ttl, out existing);
            Assert.IsTrue(added, "First insertion should succeed.");
            Assert.IsNull(existing, "No existing value expected on first insert.");

            bool found = store.TryGet("user-1", "key-123", out var value);
            Assert.IsTrue(found, "Key should be retrievable after insertion.");
            Assert.AreEqual("payload", value, "Stored payload mismatch.");
        }

        private static void TestDuplicateDetection()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromSeconds(5);
            object existing;

            bool first = store.TryAdd("service-A", "op-42", 100, ttl, out existing);
            Assert.IsTrue(first);
            bool second = store.TryAdd("service-A", "op-42", 200, ttl, out existing);
            Assert.IsFalse(second, "Second insertion of same key should be rejected.");
            Assert.IsNotNull(existing);
            Assert.AreEqual(100, existing, "Existing value should be the first payload.");

            bool get = store.TryGet("service-A", "op-42", out var value);
            Assert.IsTrue(get);
            Assert.AreEqual(100, value);
        }

        private static void TestExpiration()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromMilliseconds(200);
            object existing;

            bool added = store.TryAdd("session", "temp-key", "temp", ttl, out existing);
            Assert.IsTrue(added);
            Thread.Sleep(300); // Ensure expiration

            bool found = store.TryGet("session", "temp-key", out var value);
            Assert.IsFalse(found, "Key should have expired.");

            // After expiration, a new insertion should succeed.
            bool addedAgain = store.TryAdd("session", "temp-key", "new", ttl, out existing);
            Assert.IsTrue(addedAgain);
            Assert.IsNull(existing);
        }

        private static void TestScopingIsolation()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromSeconds(5);
            object existing;

            bool a = store.TryAdd("scopeA", "shared-key", "A", ttl, out existing);
            bool b = store.TryAdd("scopeB", "shared-key", "B", ttl, out existing);
            Assert.IsTrue(a);
            Assert.IsTrue(b);

            bool getA = store.TryGet("scopeA", "shared-key", out var valA);
            bool getB = store.TryGet("scopeB", "shared-key", out var valB);
            Assert.IsTrue(getA);
            Assert.IsTrue(getB);
            Assert.AreEqual("A", valA);
            Assert.AreEqual("B", valB);
        }

        private static void TestConcurrentAdditions()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromSeconds(5);
            int successCount = 0;
            int threadCount = 20;
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    bool added = store.TryAdd("concurrent", "key", Thread.CurrentThread.ManagedThreadId, ttl, out _);
                    if (added) Interlocked.Increment(ref successCount);
                });
                threads[i].Start();
            }
            foreach (var t in threads) t.Join();

            Assert.AreEqual(1, successCount, "Exactly one thread should succeed in adding the key.");
        }

        private static void TestCleanup()
        {
            var store = new IdempotencyKeyStore();
            var ttl = TimeSpan.FromMilliseconds(100);
            object existing;

            store.TryAdd("clean", "k1", 1, ttl, out existing);
            store.TryAdd("clean", "k2", 2, ttl, out existing);
            Thread.Sleep(150);
            store.RemoveExpired();

            bool gotK1 = store.TryGet("clean", "k1", out _);
            bool gotK2 = store.TryGet("clean", "k2", out _);
            Assert.IsFalse(gotK1);
            Assert.IsFalse(gotK2);
        }

        static void Main()
        {
            TestBasicAddAndGet();
            TestDuplicateDetection();
            TestExpiration();
            TestScopingIsolation();
            TestConcurrentAdditions();
            TestCleanup();

            Console.WriteLine("All IdempotencyKeyStore tests passed.");
        }
    }
}
