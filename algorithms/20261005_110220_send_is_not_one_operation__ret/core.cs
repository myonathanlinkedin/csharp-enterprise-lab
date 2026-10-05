using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace DistributedMessaging
{
    // Basic message contract
    public interface IMessage
    {
        Guid Id { get; }
        byte[] Payload { get; }
    }

    public sealed class Message : IMessage
    {
        public Guid Id { get; }
        public byte[] Payload { get; }

        public Message(byte[] payload)
        {
            Id = Guid.NewGuid();
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }
    }

    // Transport abstraction (in‑memory for safety)
    public interface ITransport
    {
        Task DeliverAsync(IMessage message);
        void RegisterReceiver(IReceiver receiver);
    }

    public sealed class InMemoryTransport : ITransport
    {
        private readonly ConcurrentQueue<IMessage> _queue = new();
        private IReceiver? _receiver;

        public void RegisterReceiver(IReceiver receiver) => _receiver = receiver;

        public async Task DeliverAsync(IMessage message)
        {
            if (_receiver is null)
                throw new InvalidOperationException("Receiver not registered.");

            // Simulate async delivery latency
            await Task.Yield();
            _queue.Enqueue(message);
            await _receiver.ReceiveAsync(message);
        }
    }

    // Receiver contract
    public interface IReceiver
    {
        Task ReceiveAsync(IMessage message);
    }

    public sealed class SimpleReceiver : IReceiver
    {
        private readonly ConcurrentDictionary<Guid, IMessage> _received = new();

        public IReadOnlyDictionary<Guid, IMessage> Received => _received;

        public Task ReceiveAsync(IMessage message)
        {
            _received[message.Id] = message;
            return Task.CompletedTask;
        }
    }

    // Stage contract – each stage may transform the message
    public interface IMessageStage
    {
        Task<IMessage> ProcessAsync(IMessage message);
    }

    // Serialization stage (no‑op for byte[] payload)
    public sealed class SerializationStage : IMessageStage
    {
        public Task<IMessage> ProcessAsync(IMessage message) => Task.FromResult(message);
    }

    // Transport stage – hands off to ITransport
    public sealed class TransportStage : IMessageStage
    {
        private readonly ITransport _transport;

        public TransportStage(ITransport transport) => _transport = transport;

        public async Task<IMessage> ProcessAsync(IMessage message)
        {
            await _transport.DeliverAsync(message);
            return message;
        }
    }

    // Acknowledgment stage – verifies delivery via a callback
    public sealed class AcknowledgmentStage : IMessageStage
    {
        private readonly Func<IMessage, Task<bool>> _ackFunc;

        public AcknowledgmentStage(Func<IMessage, Task<bool>> ackFunc) => _ackFunc = ackFunc;

        public async Task<IMessage> ProcessAsync(IMessage message)
        {
            bool ack = await _ackFunc(message);
            if (!ack)
                throw new InvalidOperationException($"Message {message.Id} not acknowledged.");
            return message;
        }
    }

    // Retry wrapper stage
    public sealed class RetryStage : IMessageStage
    {
        private readonly IMessageStage _inner;
        private readonly int _maxAttempts;
        private readonly TimeSpan _delay;

        public RetryStage(IMessageStage inner, int maxAttempts = 3, TimeSpan? delay = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _maxAttempts = maxAttempts;
            _delay = delay ?? TimeSpan.FromMilliseconds(10);
        }

        public async Task<IMessage> ProcessAsync(IMessage message)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await _inner.ProcessAsync(message);
                }
                catch
                {
                    attempt++;
                    if (attempt >= _maxAttempts)
                        throw;
                    await Task.Delay(_delay);
                }
            }
        }
    }

    // Sender pipeline – composes stages
    public sealed class SendPipeline : ISender
    {
        private readonly IReadOnlyList<IMessageStage> _stages;

        public SendPipeline(IEnumerable<IMessageStage> stages)
        {
            _stages = stages?.ToArray() ?? throw new ArgumentNullException(nameof(stages));
            if (_stages.Count == 0)
                throw new ArgumentException("At least one stage required.", nameof(stages));
        }

        public async Task SendAsync(IMessage message)
        {
            IMessage current = message;
            foreach (var stage in _stages)
                current = await stage.ProcessAsync(current);
        }
    }

    // Sender contract
    public interface ISender
    {
        Task SendAsync(IMessage message);
    }
}
