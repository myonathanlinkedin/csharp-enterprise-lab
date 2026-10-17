using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Idempotency;

namespace IdempotencyDemo
{
    internal static class Tests
    {
        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void RunAll()
        {
            TestSingleExecution();
            TestDuplicateSamePayload();
            TestDuplicateDifferentPayload();
            TestExpiration();
            TestConcurrentExecution();
            Console.WriteLine("All tests passed.");
        }

        private static void TestSingleExecution()
        {
            var manager = new IdempotencyManager<int>();
            int callCount = 0;
            int result = manager.Execute("key1", "hashA", () => { callCount++; return 42; });
            Assert(result == 42, "Unexpected result");
            Assert(callCount == 1, "Operation should have been executed once");
        }

        private static void TestDuplicateSamePayload()
        {
            var manager = new IdempotencyManager<string>();
            int callCount = 0;
            string first = manager.Execute("dupKey", "hashB", () => { callCount++; return "first"; });
            string second = manager.Execute("dupKey", "hashB", () => { callCount++; return "second"; });
            Assert(first == "first", "First call result mismatch");
            Assert(second == "first", "Duplicate call should return cached result");
            Assert(callCount == 1, "Operation must not be re‑executed for same payload");
        }

        private static void TestDuplicateDifferentPayload()
        {
            var manager = new IdempotencyManager<double>();
            manager.Execute("conflictKey", "hashC", () => 3.14);
            bool threw = false;
            try
            {
                manager.Execute("conflictKey", "differentHash", () => 2.71);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "Expected conflict exception for different payload hash");
        }

        private static void TestExpiration()
        {
            var manager = new IdempotencyManager<int>();
            manager.Execute("tempKey", "hashD", () => 7);
            Assert(manager.Count == 1, "Entry should exist before cleanup");
            // Simulate passage of time by adjusting internal timestamps via reflection (allowed in test only)
            var field = typeof(IdempotencyManager<int>).GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, object>)field.GetValue(manager);
            foreach (var kvp in dict)
            {
                var entryField = kvp.Value.GetType().GetField("Timestamp");
                entryField.SetValue(kvp.Value, DateTime.UtcNow - TimeSpan.FromHours(2));
            }
            manager.CleanupExpiredEntries(TimeSpan.FromHours(1));
            Assert(manager.Count == 0, "Expired entry should have been removed");
        }

        private static void TestConcurrentExecution()
        {
            var manager = new IdempotencyManager<int>();
            int executionCount = 0;
            Func<int> operation = () => { Interlocked.Increment(ref executionCount); Thread.Sleep(50); return 99; };

            const int threadCount = 10;
            var tasks = new Task<int>[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() => manager.Execute("concKey", "hashE", operation));
            }
            Task.WaitAll(tasks);
            foreach (var t in tasks) Assert(t.Result == 99, "All threads must receive same result");
            Assert(executionCount == 1, "Operation must be executed exactly once under concurrency");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Tests.RunAll();
        }
    }
}
