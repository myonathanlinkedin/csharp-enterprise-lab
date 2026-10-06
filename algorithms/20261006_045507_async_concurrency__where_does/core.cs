using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncConcurrencyDemo
{
    /// <summary>
    /// A simple single-threaded TaskScheduler that executes tasks sequentially on a dedicated thread.
    /// </summary>
    public sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SingleThreadTaskScheduler(string name = "SingleThreadTaskScheduler")
        {
            _thread = new Thread(Execute)
            {
                IsBackground = true,
                Name = name
            };
            _thread.Start();
        }

        private void Execute()
        {
            foreach (var task in _tasks.GetConsumingEnumerable(_cts.Token))
            {
                TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            if (_cts.IsCancellationRequested)
                throw new InvalidOperationException("Scheduler is disposed.");
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Inline execution is not supported; tasks must run on the dedicated thread.
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _tasks.CompleteAdding();
            _thread.Join();
            _cts.Dispose();
            _tasks.Dispose();
        }
    }

    /// <summary>
    /// Helper methods for scheduling asynchronous work on a specified TaskScheduler.
    /// </summary>
    public static class SchedulerHelper
    {
        /// <summary>
        /// Schedules an asynchronous function to run on the provided scheduler.
        /// </summary>
        public static Task RunAsync(Func<Task> asyncFunc, TaskScheduler scheduler)
        {
            if (asyncFunc == null) throw new ArgumentNullException(nameof(asyncFunc));
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = new Task(async () =>
            {
                try
                {
                    await asyncFunc().ConfigureAwait(false);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, CancellationToken.None, TaskCreationOptions.None);

            task.Start(scheduler);
            return tcs.Task;
        }
    }
}
