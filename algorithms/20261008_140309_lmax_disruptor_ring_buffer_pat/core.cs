using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// A single-producer, multi-consumer ring buffer implementing the LMAX Disruptor pattern.
    /// Uses a power-of-two capacity and atomic sequence numbers for lock-free coordination.
    /// </summary>
    public sealed class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _mask;
        private readonly int _capacity;
        private long _sequence;
        private readonly long[] _gates;
        private readonly int _consumerCount;

        public int Capacity => _capacity;
        public long Sequence => Volatile.Read(ref _sequence);

        public RingBuffer(int capacity, int consumerCount)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (consumerCount <= 0) throw new ArgumentOutOfRangeException(nameof(consumerCount));

            int size = 1;
            while (size < capacity) size <<= 1;
            _capacity = size;
            _mask = size - 1;
            _buffer = new T[size];
            _consumerCount = consumerCount;
            _gates = new long[consumerCount];
            for (int i = 0; i < consumerCount; i++)
            {
                _gates[i] = -1;
            }
            _sequence = -1;
        }

        /// <summary>
        /// Publishes an event to the next slot in the ring buffer.
        /// Returns the sequence number of the published event.
        /// Throws InvalidOperationException if the buffer is full.
        /// </summary>
        public long Publish(T value)
        {
            long current = Volatile.Read(ref _sequence);
            long next = current + 1;

            // Check if buffer is full: next sequence must not exceed the slowest consumer's gate
            long minGate = GetMinGate();
            if (next - minGate > _capacity)
            {
                throw new InvalidOperationException("Ring buffer is full.");
            }

            int index = (int)(next & _mask);
            _buffer[index] = value;

            // Publish the sequence number after writing the value
            Volatile.Write(ref _sequence, next);
            return next;
        }

        /// <summary>
        /// Attempts to publish an event without throwing. Returns false if the buffer is full.
        /// </summary>
        public bool TryPublish(T value, out long sequence)
        {
            sequence = -1;
            long current = Volatile.Read(ref _sequence);
            long next = current + 1;
            long minGate = GetMinGate();
            if (next - minGate > _capacity)
            {
                return false;
            }
            int index = (int)(next & _mask);
            _buffer[index] = value;
            Volatile.Write(ref _sequence, next);
            sequence = next;
            return true;
        }

        /// <summary>
        /// Reads the event at the given sequence number.
        /// </summary>
        public T Read(long sequence)
        {
            if (sequence < 0 || sequence > Volatile.Read(ref _sequence))
                throw new ArgumentOutOfRangeException(nameof(sequence));
            int index = (int)(sequence & _mask);
            return _buffer[index];
        }

        /// <summary>
        /// Updates the gate for a specific consumer, indicating the last sequence it has processed.
        /// </summary>
        public void UpdateGate(int consumerIndex, long processedSequence)
        {
            if (consumerIndex < 0 || consumerIndex >= _consumerCount)
                throw new ArgumentOutOfRangeException(nameof(consumerIndex));
            Volatile.Write(ref _gates[consumerIndex], processedSequence);
        }

        /// <summary>
        /// Returns the minimum gate across all consumers (the slowest consumer's position).
        /// </summary>
        public long GetMinGate()
        {
            long min = long.MaxValue;
            for (int i = 0; i < _consumerCount; i++)
            {
                long g = Volatile.Read(ref _gates[i]);
                if (g < min) min = g;
            }
            return min;
        }

        /// <summary>
        /// Returns the number of available (unconsumed) events.
        /// </summary>
        public long AvailableCount()
        {
            long seq = Volatile.Read(ref _sequence);
            long minGate = GetMinGate();
            long count = seq - minGate;
            return count > 0 ? count : 0;
        }

        /// <summary>
        /// Returns true if the buffer is full (no more events can be published).
        /// </summary>
        public bool IsFull()
        {
            long seq = Volatile.Read(ref _sequence);
            long minGate = GetMinGate();
            return (seq + 1) - minGate >= _capacity;
        }
    }
}
