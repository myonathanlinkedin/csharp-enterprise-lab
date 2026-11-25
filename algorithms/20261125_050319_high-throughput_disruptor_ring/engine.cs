using System;
using System.Threading;

namespace DisruptorDemo
{
    public sealed class Disruptor<T>
    {
        private readonly RingBuffer<T> _ringBuffer;
        private readonly Thread _consumerThread;
        private readonly Action<T> _consumerAction;
        private readonly Sequence _consumerSequence = new Sequence();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _running = true;

        public Disruptor(int bufferSize, Action<T> consumerAction)
        {
            if (consumerAction == null) throw new ArgumentNullException(nameof(consumerAction));
            _consumerAction = consumerAction;
            var gatingSequences = new Sequence[] { _consumerSequence };
            _ringBuffer = new RingBuffer<T>(bufferSize, gatingSequences);
            _consumerThread = new Thread(ConsumeLoop) { IsBackground = true };
            _consumerThread.Start();
        }

        public void Publish(T item)
        {
            long seq = _ringBuffer.Next();
            _ringBuffer.Set(seq, item);
            _ringBuffer.Publish(seq);
        }

        private void ConsumeLoop()
        {
            long seq = _consumerSequence.Value;
            try
            {
                while (_running)
                {
                    long nextSeq = seq + 1;
                    _ringBuffer.WaitFor(nextSeq, _cts.Token);
                    T item = _ringBuffer.Get(nextSeq);
                    _consumerAction(item);
                    _consumerSequence.Set(nextSeq);
                    seq = nextSeq;
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful exit
            }
        }

        public void Stop()
        {
            _running = false;
            _cts.Cancel();
            _consumerThread.Join();
        }
    }
}
