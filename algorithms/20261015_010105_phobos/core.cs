using System;
using System.Collections;
using System.Collections.Generic;

namespace PhobosDemo
{
    /// <summary>
    /// A fixed-size circular buffer (ring buffer) implementation.
    /// Provides O(1) enqueue and dequeue operations.
    /// </summary>
    /// <typeparam name="T">The type of elements stored.</typeparam>
    public sealed class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private int _head; // points to the next element to dequeue
        private int _tail; // points to the next slot for enqueue
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The maximum number of elements the buffer can hold. Must be greater than zero.</param>
        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the number of elements currently stored in the buffer.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets the total capacity of the buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Gets a value indicating whether the buffer is full.
        /// </summary>
        public bool IsFull => _count == Capacity;

        /// <summary>
        /// Gets a value indicating whether the buffer is empty.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Adds an item to the tail of the buffer.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <exception cref="InvalidOperationException">Thrown when the buffer is full.</exception>
        public void Enqueue(T item)
        {
            if (IsFull)
                throw new InvalidOperationException("Cannot enqueue to a full buffer.");

            _buffer[_tail] = item;
            _tail = (_tail + 1) % Capacity;
            _count++;
        }

        /// <summary>
        /// Removes and returns the item at the head of the buffer.
        /// </summary>
        /// <returns>The dequeued item.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the buffer is empty.</exception>
        public T Dequeue()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Cannot dequeue from an empty buffer.");

            T item = _buffer[_head];
            // Clear reference for GC safety when T is a reference type
            if (default(T) != null)
                _buffer[_head] = default!;
            _head = (_head + 1) % Capacity;
            _count--;
            return item;
        }

        /// <summary>
        /// Returns the item at the head of the buffer without removing it.
        /// </summary>
        /// <returns>The item at the head.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the buffer is empty.</exception>
        public T Peek()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Cannot peek into an empty buffer.");

            return _buffer[_head];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the buffer from head to tail.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                int index = (_head + i) % Capacity;
                yield return _buffer[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
