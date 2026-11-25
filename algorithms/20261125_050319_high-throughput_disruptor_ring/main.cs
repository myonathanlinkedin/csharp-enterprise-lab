using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DisruptorDemo
{
    public static class Program
    {
        public static void Main()
        {
            const int itemCount = 10000;
            const int bufferSize = 1024; // must be power of 2

            var consumed = new List<int>(itemCount);
            var consumeLock = new object();

            var disruptor = new Disruptor<int>(bufferSize, item =>
            {
                lock (consumeLock)
                {
                    consumed.Add(item);
                }
            });

            var producerThread = new Thread(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    disruptor.Publish(i);
                }
            });

            producerThread.Start();
            producerThread.Join();

            // Wait until all items are consumed
            while (true)
            {
                lock (consumeLock)
                {
                    if (consumed.Count >= itemCount)
                        break;
                }
                Thread.Sleep(1);
            }

            disruptor.Stop();

            // Assertions
            Debug.Assert(consumed.Count == itemCount, $"Consumed count {consumed.Count} != {itemCount}");
            for (int i = 0; i < itemCount; i++)
            {
                Debug.Assert(consumed[i] == i, $"Item at index {i} is {consumed[i]} != {i}");
            }

            Console.WriteLine("All assertions passed. Disruptor test succeeded.");
        }
    }
}
