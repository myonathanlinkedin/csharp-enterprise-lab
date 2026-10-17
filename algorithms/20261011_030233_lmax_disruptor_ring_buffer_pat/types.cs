using System;

namespace DisruptorDemo
{
    /// <summary>
    /// Configuration options for the RingBuffer.
    /// </summary>
    public sealed class RingBufferOptions
    {
        /// <summary>
        /// Size of the ring buffer. Must be a power of two and greater than zero.
        /// </summary>
        public int BufferSize { get; }

        public RingBufferOptions(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException("Buffer size must be positive.", nameof(bufferSize));
            if ((bufferSize & (bufferSize - 1)) != 0)
                throw new ArgumentException("Buffer size must be a power of two.", nameof(bufferSize));

            BufferSize = bufferSize;
        }
    }
}
