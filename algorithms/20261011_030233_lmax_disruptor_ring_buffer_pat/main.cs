using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DisruptorDemo
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception("Assertion failed: " + message);
        }

        private static void SingleThreadedTest()
        {
            var options = new RingBufferOptions(8);
            var rb = new RingBuffer<int>(options);

            // Publish 5 items.
            for (int i = 0; i < 5; i++)
                Assert(rb.TryPublish(i), $"Publish {i} should succeed");

            // Consume them in order.
            for (int i = 0; i < 5; i++)
            {
                bool ok = rb.TryConsume(out int value);
                Assert(ok, $"Consume {i} should succeed");
                Assert(value == i, $"Expected {i}, got {value}");
            }

            // Buffer should now be empty.
            Assert(!rb.TryConsume(out _), "Buffer should be empty after consuming all items");
        }

        private static void WrapAroundTest()
        {
            var options = new RingBufferOptions(4); // small power‑of‑two buffer
            var rb = new RingBuffer<int>(options);

            // Fill the buffer completely.
            for (int i = 0; i < 4; i++)
                Assert(rb.TryPublish(i), $"Publish {i} should succeed");

            // Buffer is full; next publish must fail.
            Assert(!rb.TryPublish(99), "Publish should fail when buffer is full");

            // Consume two items.
            for (int i = 0; i < 2; i++)
            {
                bool ok = rb.TryConsume(out int v);
                Assert(ok, $"Consume {i} should succeed");
                Assert(v == i, $"Expected {i}, got {v}");
            }

            // Now there is space for two more items (wrap‑around).
            for (int i = 4; i < 6; i++)
                Assert(rb.TryPublish(i), $"Publish {i} after wrap should succeed");

            // Consume remaining items and verify order.
            int[] expected = { 2, 3, 4, 5 };
            foreach (int exp in expected)
            {
                bool ok = rb.TryConsume(out int v);
                Assert(ok, $"Consume expected {exp} should succeed");
                Assert(v == exp, $"Expected {exp}, got {v}");
            }

            Assert(!rb.TryConsume(out _), "Buffer should be empty after wrap‑around test");
        }

        private static void MultiThreadedTest()
        {
            const int bufferSize = 1024;
            const int itemCount = 100_000;

            var options = new RingBufferOptions(bufferSize);
            var rb = new RingBuffer<int>(options);

            var produced = new List<int>(itemCount);
            var consumed = new List<int>(itemCount);
            var prodLock = new object();
            var consLock = new object();

            // Producer task
            Task producer = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    // Spin until we can publish (buffer may be full temporarily).
                    while (!rb.TryPublish(i))
                        Thread.Yield();

                    lock (prodLock)
                        produced.Add(i);
                }
            });

            // Consumer task
            Task consumer = Task.Run(() =>
            {
                int received = 0;
                while (received < itemCount)
                {
                    if (rb.TryConsume(out int value))
                    {
                        lock (consLock)
                            consumed.Add(value);
                        received++;
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            });

            Task.WaitAll(producer, consumer);

            // Verify counts.
            Assert(produced.Count == itemCount, $"Produced count {produced.Count} != {itemCount}");
            Assert(consumed.Count == itemCount, $"Consumed count {consumed.Count} != {itemCount}");

            // Verify order preservation.
            for (int i = 0; i < itemCount; i++)
                Assert(consumed[i] == i, $"Item at position {i} should be {i}, got {consumed[i]}");
        }

        private static void RunAllTests()
        {
            SingleThreadedTest();
            WrapAroundTest();
            MultiThreadedTest();

            Console.WriteLine("All Disruptor ring‑buffer tests passed.");
        }

        static void Main()
        {
            RunAllTests();
        }
    }
}
