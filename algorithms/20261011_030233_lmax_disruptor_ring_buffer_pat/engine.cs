using System;
using System.Threading;

namespace DisruptorDemo
{
    /// <summary>
    /// A lock‑free single‑producer / single‑consumer ring buffer inspired by the LMAX Disruptor pattern.
    /// </summary>
    /// <typeparam name="T">The type of the stored items. Must be a reference or value type.</typeparam>
    public sealed class RingBuffer<T>
    {
        private readonly T[] _entries;
        private readonly int _bufferSize;
        private readonly long _indexMask;

        // Producer state
        private long _nextSequence = 0;          // next sequence to claim (incremented atomically)
        private long _publishedSequence = -1;    // highest sequence that has been published (volatile)

        // Consumer state
        private long _consumerSequence = -1;     // highest sequence that has been consumed (volatile)

        public RingBuffer(RingBufferOptions options)
        {
            _bufferSize = options.BufferSize;
            _entries = new T[_bufferSize];
            _indexMask = _bufferSize - 1L;
        }

        /// <summary>
        /// Attempts to publish an item into the ring buffer.
        /// Returns true if the item was published; false if the buffer is full.
        /// </summary>
        public bool TryPublish(T item)
        {
            // Claim the next sequence atomically.
            long seq = Interlocked.Increment(ref _nextSequence) - 1;

            // Determine the wrap point: the sequence that would overwrite the oldest unread entry.
            long wrapPoint = seq - _bufferSize;

            // If the consumer has not yet advanced past the wrap point, the buffer is full.
            // In that case we roll back the claim and return false.
            if (wrapPoint > Volatile.Read(ref _consumerSequence))
            {
                // Roll back the claim.
                Interlocked.Decrement(ref _nextSequence);
                return false;
            }

            // Store the item into the slot.
            int index = (int)(seq & _indexMask);
            _entries[index] = item;

            // Publish the sequence so the consumer can see the item.
            // The write to the entry must happen-before this store.
            Volatile.Write(ref _publishedSequence, seq);
            return true;
        }

        /// <summary>
        /// Attempts to consume the next available item.
        /// Returns true and sets <paramref name="item"/> if an item was available; otherwise false.
        /// </summary>
        public bool TryConsume(out T item)
        {
            long nextSeq = _consumerSequence + 1;
            long published = Volatile.Read(ref _publishedSequence);

            if (nextSeq <= published)
            {
                int index = (int)(nextSeq & _indexMask);
                item = _entries[index];
                // Advance the consumer sequence.
                _consumerSequence = nextSeq;
                return true;
            }

            item = default!;
            return false;
        }

        /// <summary>
        /// Returns the current number of items that are available for consumption.
        /// </summary>
        public long Available()
        {
            long published = Volatile.Read(ref _publishedSequence);
            return published - _consumerSequence;
        }

        /// <summary>
        /// Returns the current capacity of the buffer (its size).
        /// </summary>
        public int Capacity => _bufferSize;
    }
}
