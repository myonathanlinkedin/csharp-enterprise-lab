using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsyncConcurrencyDemo;

namespace AsyncConcurrencyDemo
{
    internal static class Program
    {
        private static void Main()
        {
            RunTests();
            RunBenchmarks();
            Console.WriteLine("All tests passed.");
        }

        private static void RunTests()
        {
            TestSingleThreadScheduler();
            TestDefaultScheduler();
        }

        private static void TestSingleThreadScheduler()
        {
            using var scheduler = new SingleThreadTaskScheduler();
            var threadIds = new ConcurrentBag<int>();
            var tasks = new Task[5];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = SchedulerHelper.RunAsync(async () =>
                {
                    await Task.Yield(); // ensure async boundary
                    threadIds.Add(Thread.CurrentThread.ManagedThreadId);
                }, scheduler);
            }

            Task.WaitAll(tasks);

            // All thread IDs should be identical
            int firstId = -1;
            foreach (var id in threadIds)
            {
                if (firstId == -1) firstId = id;
                Debug.Assert(id == firstId, "Tasks did not run on the same thread.");
            }
        }

        private static void TestDefaultScheduler()
        {
            var threadIds = new ConcurrentBag<int>();
            var tasks = new Task[5];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = SchedulerHelper.RunAsync(async () =>
                {
                    await Task.Yield();
                    threadIds.Add(Thread.CurrentThread.ManagedThreadId);
                }, TaskScheduler.Default);
            }

            Task.WaitAll(tasks);

            // Thread IDs should not all be the same
            int? firstId = null;
            foreach (var id in threadIds)
            {
                if (firstId == null) firstId = id;
                else if (id != firstId) return; // success
            }

            throw new Exception("Default scheduler tasks ran on the same thread, expected variability.");
        }

        private static void RunBenchmarks()
        {
            const int iterations = 100_000;
            var sw = Stopwatch.StartNew();

            // Benchmark default scheduler
            var defaultTasks = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                defaultTasks[i] = Task.Run(() => { /* trivial work */ });
            }
            Task.WaitAll(defaultTasks);
            sw.Stop();
            Console.WriteLine($"Default scheduler: {sw.ElapsedMilliseconds} ms for {iterations} tasks.");

            // Benchmark single-thread scheduler
            using var scheduler = new SingleThreadTaskScheduler();
            sw.Restart();
            var singleTasks = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                singleTasks[i] = SchedulerHelper.RunAsync(async () => { await Task.Yield(); }, scheduler);
            }
            Task.WaitAll(singleTasks);
            sw.Stop();
            Console.WriteLine($"Single-thread scheduler: {sw.ElapsedMilliseconds} ms for {iterations} tasks.");
        }
    }
}
