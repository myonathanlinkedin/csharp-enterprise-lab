using System;
using System.Threading;

namespace CowVectorNS
{
    // Internal buffer that holds the actual array and a reference count.
    internal sealed class Buffer<T>
    {
        public T[] Data;
        private int _refCount;

        public Buffer(int capacity)
        {
            Data = new T[capacity];
            _refCount = 1;
        }

        public Buffer(T[] source, int length)
        {
            Data = new T[source.Length];
            Array.Copy(source, Data, length);
            _refCount = 1;
        }

        // Increment reference count atomically.
        public void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        // Decrement reference count atomically and return true if no more references.
        public bool Release()
        {
            return Interlocked.Decrement(ref _refCount) == 0;
        }

        // Current reference count (for debugging / testing).
        public int RefCount => Volatile.Read(ref _refCount);
    }

    // Public copy‑on‑write vector.
    public sealed class CowVector<T>
    {
        private Buffer<T> _buffer;
        private int _size; // logical number of elements

        // Constructors
        public CowVector()
        {
            _buffer = new Buffer<T>(4);
            _size = 0;
        }

        public CowVector(T[] source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _buffer = new Buffer<T>(source, source.Length);
            _size = source.Length;
        }

        private CowVector(Buffer<T> buffer, int size)
        {
            _buffer = buffer;
            _size = size;
        }

        // Number of stored elements.
        public int Count => _size;

        // Indexer – read/write with copy‑on‑write semantics.
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _buffer.Data[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException(nameof(index));
                EnsureUnique();
                _buffer.Data[index] = value;
            }
        }

        // Append a new element.
        public void Add(T item)
        {
            EnsureUnique();
            EnsureCapacity(_size + 1);
            _buffer.Data[_size++] = item;
        }

        // Remove element at position index, shifting left.
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
                throw new ArgumentOutOfRangeException(nameof(index));
            EnsureUnique();
            int moveCount = _size - index - 1;
            if (moveCount > 0)
                Array.Copy(_buffer.Data, index + 1, _buffer.Data, index, moveCount);
            _buffer.Data[--_size] = default!; // clear last slot
        }

        // Return a shallow copy that shares the underlying buffer.
        public CowVector<T> Clone()
        {
            _buffer.AddRef();
            return new CowVector<T>(_buffer, _size);
        }

        // Convert to a plain array (always a fresh copy).
        public T[] ToArray()
        {
            T[] result = new T[_size];
            Array.Copy(_buffer.Data, result, _size);
            return result;
        }

        // Ensure the underlying buffer is uniquely owned before mutation.
        private void EnsureUnique()
        {
            if (_buffer.RefCount == 1) return;

            // Create a new buffer with the same logical size.
            var newBuf = new Buffer<T>(_size);
            Array.Copy(_buffer.Data, newBuf.Data, _size);
            // Release old buffer reference.
            _buffer.Release();
            _buffer = newBuf;
        }

        // Grow internal array if needed.
        private void EnsureCapacity(int required)
        {
            if (required <= _buffer.Data.Length) return;
            int newCap = _buffer.Data.Length * 2;
            if (newCap < required) newCap = required;
            var newBuf = new Buffer<T>(newCap);
            Array.Copy(_buffer.Data, newBuf.Data, _size);
            // Release old buffer (its refCount is at least 1 because we own it after EnsureUnique)
            _buffer.Release();
            _buffer = newBuf;
        }
    }
}
