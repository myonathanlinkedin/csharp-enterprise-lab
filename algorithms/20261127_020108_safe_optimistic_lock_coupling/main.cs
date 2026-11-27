using System;
using System.Collections.Generic;
using System.Threading;
using OptimisticLockCoupling;

namespace OptimisticLockCouplingTest
{
    internal static class Assert
    {
        public static void That(bool condition, string message = "Assertion failed")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public static void Equal<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Assert.Equal failed. Expected: {expected}, Actual: {actual}. {message}");
        }
    }

    internal static class Tests
    {
        public static void RunAll()
        {
            TestSingleThreadedOperations();
            TestOptimisticRead();
            TestConcurrentInsertions();
            TestConcurrentInsertDelete();
            Console.WriteLine("All tests passed.");
        }

        private static void TestSingleThreadedOperations()
        {
            var list = new OptimisticLinkedList<int, string>();

            // Add
            Assert.That(list.TryAdd(10, "ten"));
            Assert.That(list.TryAdd(5, "five"));
            Assert.That(list.TryAdd(20, "twenty"));
            // Duplicate add
            Assert.That(!list.TryAdd(10, "duplicate"));

            // Get
            Assert.That(list.TryGetValue(5, out var v5) && v5 == "five");
            Assert.That(list.TryGetValue(10, out var v10) && v10 == "ten");
            Assert.That(list.TryGetValue(20, out var v20) && v20 == "twenty");
            Assert.That(!list.TryGetValue(15, out _));

            // Remove
            Assert.That(list.TryRemove(10, out var removed));
            Assert.Equal("ten", removed);
            Assert.That(!list.TryGetValue(10, out _));
            // Remove non‑existent
            Assert.That(!list.TryRemove(30, out _));

            // Verify order
            var expected = new[] { (5, "five"), (20, "twenty") };
            int idx = 0;
            foreach (var kv in list.Enumerate())
            {
                Assert.Equal(expected[idx].Item1, kv.Key);
                Assert.Equal(expected[idx].Item2, kv.Value);
                idx++;
            }
            Assert.Equal(expected.Length, idx);
        }

        private static void TestOptimisticRead()
        {
            var list = new OptimisticLinkedList<int, int>();
            for (int i = 0; i < 1000; i++)
                Assert.That(list.TryAdd(i, i * i));

            // Perform many optimistic reads in parallel with a writer thread
            var stop = false;
            var reader = new Thread(() =>
            {
                while (!stop)
                {
                    int key = new Random().Next(0, 1000);
                    if (list.TryGetValue(key, out var val))
                        Assert.Equal(key * key, val);
                }
            });

            var writer = new Thread(() =>
            {
                for (int i = 1000; i < 1100; i++)
                {
                    list.TryAdd(i, i * i);
                    Thread.Sleep(1);
                }
            });

            reader.Start();
            writer.Start();
            writer.Join();
            stop = true;
            reader.Join();
        }

        private static void TestConcurrentInsertions()
        {
            var list = new OptimisticLinkedList<int, int>();
            const int threadCount = 4;
            const int perThread = 2500;
            Thread[] threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int offset = t * perThread;
                threads[t] = new Thread(() =>
                {
                    for (int i = 0; i < perThread; i++)
                    {
                        int key = offset + i;
                        Assert.That(list.TryAdd(key, key));
                    }
                });
                threads[t].Start();
            }

            foreach (var th in threads) th.Join();

            // Verify all keys present and sorted
            int expectedKey = 0;
            foreach (var kv in list.Enumerate())
            {
                Assert.Equal(expectedKey, kv.Key);
                Assert.Equal(expectedKey, kv.Value);
                expectedKey++;
            }
            Assert.Equal(threadCount * perThread, expectedKey);
        }

        private static void TestConcurrentInsertDelete()
        {
            var list = new OptimisticLinkedList<int, int>();
            const int total = 2000;
            for (int i = 0; i < total; i++)
                Assert.That(list.TryAdd(i, i));

            const int threadCount = 4;
            Thread[] threads = new Thread[threadCount];

            // Each thread removes a distinct slice
            for (int t = 0; t < threadCount; t++)
            {
                int start = t * (total / threadCount);
                int end = start + (total / threadCount);
                threads[t] = new Thread(() =>
                {
                    for (int i = start; i < end; i++)
                    {
                        Assert.That(list.TryRemove(i, out var val));
                        Assert.Equal(i, val);
                    }
                });
                threads[t].Start();
            }

            foreach (var th in threads) th.Join();

            // List should now be empty
            Assert.Equal(0, CountElements(list));
        }

        private static int CountElements(OptimisticLinkedList<int, int> list)
        {
            int cnt = 0;
            foreach (var _ in list.Enumerate())
                cnt++;
            return cnt;
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
