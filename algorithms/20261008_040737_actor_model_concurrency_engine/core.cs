using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ActorModel
{
    // Base class for actors processing messages of type TMessage.
    public abstract class Actor<TMessage> : IAsyncDisposable
    {
        private readonly ConcurrentQueue<TMessage> _mailbox = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _processingTask;

        protected Actor()
        {
            _processingTask = Task.Run(ProcessLoopAsync);
        }

        // Enqueue a message to the actor's mailbox.
        public void Send(TMessage message)
        {
            _mailbox.Enqueue(message);
            _signal.Release();
        }

        // Core processing loop.
        private async Task ProcessLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    await _signal.WaitAsync(_cts.Token).ConfigureAwait(false);
                    while (_mailbox.TryDequeue(out var msg))
                    {
                        await ReceiveAsync(msg).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        // Implemented by concrete actors to handle a single message.
        protected abstract Task ReceiveAsync(TMessage message);

        // Gracefully stop the actor.
        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _signal.Release(); // Unblock if waiting.
            await _processingTask.ConfigureAwait(false);
            _cts.Dispose();
            _signal.Dispose();
        }
    }

    // Example concrete actor that counts integer messages.
    public sealed class CounterActor : Actor<int>
    {
        private int _count;

        public int Count => _count;

        protected override Task ReceiveAsync(int message)
        {
            Interlocked.Add(ref _count, message);
            return Task.CompletedTask;
        }
    }

    // Simple ping-pong actors demonstrating inter-actor messaging.
    public sealed class PingActor : Actor<string>
    {
        private readonly PongActor _pong;
        private readonly TaskCompletionSource<bool> _tcs = new();

        public PingActor(PongActor pong) => _pong = pong;

        public Task Completion => _tcs.Task;

        protected override Task ReceiveAsync(string message)
        {
            if (message == "start")
            {
                _pong.Send("ping");
            }
            else if (message == "pong")
            {
                _tcs.TrySetResult(true);
            }
            return Task.CompletedTask;
        }
    }

    public sealed class PongActor : Actor<string>
    {
        private readonly PingActor _ping;

        public PongActor(PingActor ping) => _ping = ping;

        protected override Task ReceiveAsync(string message)
        {
            if (message == "ping")
            {
                _ping.Send("pong");
            }
            return Task.CompletedTask;
        }
    }
}
