using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Disruptor;

namespace Disruptor.Tests
{
    public static class Program
    {
        private static int _passed;
        private static int _failed;

        private static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  PASS: {testName}");
            }
            else
            {
                _failed++;
                Console.WriteLine($"  FAIL: {testName}");
            }
        }

        private static void RunTests()
        {
            Console.WriteLine("=== Disruptor Ring Buffer Unit Tests ===\n");

            // Test 1: Basic publish and read
            {
                var rb = new RingBuffer<int>(4, 1);
                long s0 = rb.Publish(10);
                long s1 = rb.Publish(20);
                long s2 = rb.Publish(30);
                Assert(s0 == 0, "First sequence is 0");
                Assert(s1 == 1, "Second sequence is 1");
                Assert(s2 == 2, "Third sequence is 2");
                Assert(rb.Read(0) == 10, "Read seq 0 returns 10");
                Assert(rb.Read(1) == 20, "Read seq 1 returns 20");
                Assert(rb.Read(2) == 30, "Read seq 2 returns 30");
            }

            // Test 2: Capacity rounding to power of two
            {
                var rb = new RingBuffer<int>(5, 1);
                Assert(rb.Capacity == 8, "Capacity 5 rounds up to 8");
            }

            // Test 3: Buffer full detection
            {
                var rb = new RingBuffer<int>(4, 1);
                rb.Publish(1);
                rb.Publish(2);
                rb.Publish(3);
                rb.Publish(4);
                Assert(rb.IsFull(), "Buffer is full after 4 publishes");
                bool threw = false;
                try { rb.Publish(5); } catch (InvalidOperationException) { threw = true; }
                Assert(threw, "Publish throws when full");
                Assert(!rb.TryPublish(5, out _), "TryPublish returns false when full");
            }

            // Test 4: Consumer gate updates allow wrap-around
            {
                var rb = new RingBuffer<int>(4, 1);
                rb.Publish(1);
                rb.Publish(2);
                rb.Publish(3);
                rb.Publish(4);
                Assert(rb.IsFull(), "Full before gate update");
                rb.UpdateGate(0, 1); // Consumer processed up to seq 1
                Assert(!rb.IsFull(), "Not full after gate update");
                long s = rb.Publish(5);
                Assert(s == 4, "New publish gets seq 4");
                Assert(rb.Read(4) == 5, "Read seq 4 returns 5");
            }

            // Test 5: Multi-consumer min gate
            {
                var rb = new RingBuffer<int>(8, 3);
                for (int i = 0; i < 8; i++) rb.Publish(i);
                Assert(rb.IsFull(), "Full with 3 consumers");
                rb.UpdateGate(0, 3);
                rb.UpdateGate(1, 5);
                rb.UpdateGate(2, 2);
                Assert(rb.GetMinGate() == 2, "Min gate is 2 (slowest consumer)");
                Assert(rb.IsFull(), "Still full because min gate is 2");
                rb.UpdateGate(2, 4);
                Assert(rb.GetMinGate() == 3, "Min gate is now 3");
                Assert(!rb.IsFull(), "Not full after slowest consumer advances");
            }

            // Test 6: Available count
            {
                var rb = new RingBuffer<int>(8, 1);
                Assert(rb.AvailableCount() == 0, "Initial available count is 0");
                rb.Publish(1);
                rb.Publish(2);
                rb.Publish(3);
                Assert(rb.AvailableCount() == 3, "Available count is 3");
                rb.UpdateGate(0, 1);
                Assert(rb.AvailableCount() == 2, "Available count is 2 after gate update");
            }

            // Test 7: Invalid arguments
            {
                bool threw = false;
                try { new RingBuffer<int>(0, 1); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Zero capacity throws");
                threw = false;
                try { new RingBuffer<int>(4, 0); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Zero consumers throws");
            }

            // Test 8: Read out of range
            {
                var rb = new RingBuffer<int>(4, 1);
                rb.Publish(1);
                bool threw = false;
                try { rb.Read(5); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Read beyond sequence throws");
                threw = false;
                try { rb.Read(-1); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Read negative sequence throws");
            }

            // Test 9: Invalid consumer index
            {
                var rb = new RingBuffer<int>(4, 2);
                bool threw = false;
                try { rb.UpdateGate(5, 0); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Invalid consumer index throws");
            }

            // Test 10: Wrap-around correctness
            {
                var rb = new RingBuffer<int>(4, 1);
                for (int i = 0; i < 4; i++) rb.Publish(i);
                rb.UpdateGate(0, 3);
                for (int i = 4; i < 8; i++) rb.Publish(i);
                Assert(rb.Read(4) == 4, "Wrap-around read seq 4");
                Assert(rb.Read(7) == 7, "Wrap-around read seq 7");
            }

            // Test 11: Sequence monotonicity
            {
                var rb = new RingBuffer<int>(16, 1);
                long prev = -1;
                bool monotonic = true;
                for (int i = 0; i < 16; i++)
                {
                    long s = rb.Publish(i);
                    if (s <= prev) monotonic = false;
                    prev = s;
                }
                Assert(monotonic, "Sequence numbers are strictly increasing");
            }

            // Test 12: Concurrent producer-consumer stress test
            {
                int capacity = 1024;
                int consumerCount = 2;
                int totalEvents = 10000;
                var rb = new RingBuffer<int>(capacity, consumerCount);
                var results = new List<int>();
                var lockObj = new object();
                var done = new ManualResetEventSlim(false);

                // Producer
                var producer = new Thread(() =>
                {
                    for (int i = 0; i < totalEvents; i++)
                    {
                        while (!rb.TryPublish(i, out _))
                        {
                            Thread.SpinWait(10);
                        }
                    }
                    done.Set();
                });

                // Consumers
                var consumers = new List<Thread>();
                for (int c = 0; c < consumerCount; c++)
                {
                    int idx = c;
                    var consumer = new Thread(() =>
                    {
                        long lastProcessed = -1;
                        while (true)
                        {
                            long seq = rb.Sequence;
                            if (seq > lastProcessed)
                            {
                                for (long s = lastProcessed + 1; s <= seq; s++)
                                {
                                    int val = rb.Read(s);
                                    lock (lockObj) { results.Add(val); }
                                    lastProcessed = s;
                                }
                                rb.UpdateGate(idx, lastProcessed);
                            }
                            if (done.IsSet && lastProcessed >= totalEvents - 1)
                                break;
                            Thread.SpinWait(10);
                        }
                    });
                    consumers.Add(consumer);
                }

                producer.Start();
                foreach (var c in consumers) c.Start();
                producer.Join();
                foreach (var c in consumers) c.Join();

                lock (lockObj)
                {
                    Assert(results.Count == totalEvents, $"All {totalEvents} events received (got {results.Count})");
                    // Verify no duplicates and all values present
                    var set = new HashSet<int>(results);
                    Assert(set.Count == totalEvents, "No duplicate events");
                }
            }

            // Test 13: Benchmark - throughput measurement
            {
                int capacity = 4096;
                var rb = new RingBuffer<int>(capacity, 1);
                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    while (!rb.TryPublish(i, out _))
                        Thread.SpinWait(1);
                    rb.UpdateGate(0, rb.Sequence);
                }
                sw.Stop();
                double opsPerSec = iterations / (sw.Elapsed.TotalSeconds);
                Console.WriteLine($"\n  Benchmark: {iterations:N0} ops in {sw.ElapsedMilliseconds} ms ({opsPerSec:N0} ops/sec)");
                Assert(sw.ElapsedMilliseconds > 0, "Benchmark completed");
            }

            Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===");
        }

        public static int Main()
        {
            RunTests();
            return _failed == 0 ? 0 : 1;
        }
    }
}
