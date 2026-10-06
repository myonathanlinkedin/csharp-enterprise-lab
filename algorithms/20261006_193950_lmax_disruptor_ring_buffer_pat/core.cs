using System;
using System.Threading;

namespace DisruptorDemo
{
    public class RingBuffer<T>
    {
        private readonly int _size;
        private readonly int _mask;
        private readonly T[] _entries;
        private long _nextSequence = -1;
        private long _publishedSequence = -1;
        private readonly ManualResetEventSlim _publishEvent = new ManualResetEventSlim(false);

        public RingBuffer(int size)
        {
            if ((size & (size - 1)) != 0) throw new ArgumentException("size must be power of two");
            _size = size;
            _mask = size - 1;
            _entries = new T[size];
        }

        public long Next()
        {
            return Interlocked.Increment(ref _nextSequence);
        }

        public void Publish(long sequence, T value)
        {
            _entries[(int)(sequence & _mask)] = value;
            Interlocked.Exchange(ref _publishedSequence, sequence);
            _publishEvent.Set();
        }

        public T Get(long sequence)
        {
            while (Interlocked.Read(ref _publishedSequence) < sequence)
            {
                _publishEvent.Wait(1);
            }
            return _entries[(int)(sequence & _mask)];
        }
    }
}
