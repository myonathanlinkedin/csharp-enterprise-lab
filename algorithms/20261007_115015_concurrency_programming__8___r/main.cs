using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ReadWriteLockDemo
{
    internal static class Program
    {
        private static readonly ReadWriteLock RwLock = new ReadWriteLock();

        // Tracking variables used only for assertions inside the test harness.
        private static int _activeReaders = 0;
        private static int _writerActive = 0; // 0 = no writer, 1 = writer active

        private static void AssertInvariant()
        {
            // No writer should be active while any reader is active.
            Debug.Assert(_writerActive == 0 || _activeReaders == 0,
                "Invariant violated: writer active together with readers.");
            // Reader count should never be negative.
            Debug.Assert(_activeReaders >= 0, "Invariant violated: negative reader count.");
            // Writer flag should be 0 or 1.
            Debug.Assert(_writerActive == 0 || _writerActive == 1,
                "Invariant violated: writer flag out of range.");
        }

        private static async Task ReaderTask(int id, int workMs)
        {
            RwLock.EnterReadLock();
            try
            {
                int readersNow = Interlocked.Increment(ref _activeReaders);
                Debug.Assert(readersNow > 0, "Reader count should be positive after increment.");
                AssertInvariant();

                // Simulate read work.
                await Task.Delay(workMs);
            }
            finally
            {
                int readersNow = Interlocked.Decrement(ref _activeReaders);
                Debug.Assert(readersNow >= 0, "Reader count should not go negative after decrement.");
                AssertInvariant();
                RwLock.ExitReadLock();
            }
        }

        private static async Task WriterTask(int id, int workMs)
        {
            RwLock.EnterWriteLock();
            try
            {
                int writerNow = Interlocked.Exchange(ref _writerActive, 1);
                Debug.Assert(writerNow == 0, "Writer flag should have been 0 before acquisition.");
                AssertInvariant();

                // Simulate write work.
                await Task.Delay(workMs);
            }
            finally
            {
                int writerNow = Interlocked.Exchange(ref _writerActive, 0);
                Debug.Assert(writerNow == 1, "Writer flag should have been 1 before release.");
                AssertInvariant();
                RwLock.ExitWriteLock();
            }
        }

        private static async Task RunTests()
        {
            const int readerCount = 8;
            const int writerCount = 2;
            const int readerWorkMs = 100;
            const int writerWorkMs = 150;

            var tasks = new List<Task>();

            // Launch readers.
            for (int i = 0; i < readerCount; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() => ReaderTask(id, readerWorkMs)));
            }

            // Interleave writers among readers.
            for (int i = 0; i < writerCount; i++)
            {
                int id = i;
                // Slight delay to increase contention.
                await Task.Delay(30);
                tasks.Add(Task.Run(() => WriterTask(id, writerWorkMs)));
            }

            // Wait for all to finish.
            await Task.WhenAll(tasks);

            // Final invariant check.
            AssertInvariant();

            // Verify lock state is clean.
            Debug.Assert(RwLock.CurrentReaderCount == 0, "Lock should have zero readers after all work.");
            Debug.Assert(!RwLock.IsWriterActive, "Lock should have no writer after all work.");
        }

        public static void Main()
        {
            // Run the async test harness synchronously.
            RunTests().GetAwaiter().GetResult();

            // Simple demonstration output.
            Console.WriteLine("All read‑write lock tests passed.");
        }
    }
}
