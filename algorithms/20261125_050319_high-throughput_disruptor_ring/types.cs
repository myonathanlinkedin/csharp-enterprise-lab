using System;
using System.Threading;

namespace DisruptorDemo
{
    public sealed class Sequence
    {
        private long _value;

        public Sequence(long initialValue = -1)
        {
            _value = initialValue;
        }

        public long Value => Volatile.Read(ref _value);

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref _value);
        }

        public void Set(long value)
        {
            Volatile.Write(ref _value, value);
        }
    }

    public sealed class RingBuffer<T>
    {
        private readonly T[] _entries;
        private readonly int _mask;
        private readonly Sequence _cursor = new Sequence();
        private readonly Sequence[] _gatingSequences;

        public RingBuffer(int bufferSize, Sequence[] gatingSequences)
        {
            if (bufferSize <= 0 || (bufferSize & (bufferSize - 1)) != 0)
                throw new ArgumentException("bufferSize must be power of 2", nameof(bufferSize));

            _entries = new T[bufferSize];
            _mask = bufferSize - 1;
            _gatingSequences = gatingSequences ?? Array.Empty<Sequence>();
        }

        public long Next()
        {
            long nextSequence = _cursor.IncrementAndGet();
            long wrapPoint = nextSequence - (_entries.Length);
            foreach (var gatingSequence in _gatingSequences)
            {
                long gatingValue = gatingSequence.Value;
                if (wrapPoint > gatingValue)
                {
                    while (wrapPoint > gatingSequence.Value)
                    {
                        Thread.SpinWait(1);
                    }
                }
            }
            return nextSequence;
        }

        public void Publish(long sequence)
        {
            // Publishing is a no-op in this simplified implementation
        }

        public T Get(long sequence)
        {
            return _entries[(int)(sequence & _mask)];
        }

        public void Set(long sequence, T value)
        {
            _entries[(int)(sequence & _mask)] = value;
        }

        public long WaitFor(long sequence)
        {
            while (_cursor.Value < sequence)
            {
                Thread.SpinWait(1);
            }
            return _cursor.Value;
        }

        public long WaitFor(long sequence, CancellationToken token)
        {
            while (_cursor.Value < sequence)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);
                Thread.SpinWait(1);
            }
            return _cursor.Value;
        }
    }
}
