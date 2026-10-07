using System;
using System.Threading;

namespace ReadWriteLockDemo
{
    /// <summary>
    /// A lightweight reader‑writer lock based on atomic operations.
    /// Multiple readers may hold the lock concurrently.
    /// A writer obtains exclusive access.
    /// </summary>
    public sealed class ReadWriteLock
    {
        // State layout:
        // bits 0‑29 : number of readers (0‑2^30‑1)
        // bit 30    : writer flag (0 = no writer, 1 = writer holds lock)
        // bit 31    : unused
        private int _state;

        /// <summary>
        /// Acquires the lock for reading. Blocks until no writer holds the lock.
        /// </summary>
        public void EnterReadLock()
        {
            while (true)
            {
                int snapshot = Volatile.Read(ref _state);
                // If a writer is present, spin.
                if ((snapshot & LockConstants.WriterMask) != 0)
                {
                    Thread.Yield();
                    continue;
                }

                // Ensure we don't overflow the reader count.
                if ((snapshot & LockConstants.ReaderMask) == LockConstants.ReaderMask)
                {
                    throw new InvalidOperationException("Reader count overflow.");
                }

                int newState = snapshot + 1; // increment reader count
                if (Interlocked.CompareExchange(ref _state, newState, snapshot) == snapshot)
                {
                    // Successfully acquired read lock.
                    return;
                }

                // CAS failed – another thread changed the state; retry.
                Thread.Yield();
            }
        }

        /// <summary>
        /// Releases a previously acquired read lock.
        /// </summary>
        public void ExitReadLock()
        {
            while (true)
            {
                int snapshot = Volatile.Read(ref _state);
                int readerCount = snapshot & LockConstants.ReaderMask;

                if (readerCount == 0)
                {
                    throw new InvalidOperationException("Attempt to release a read lock that was not held.");
                }

                int newState = snapshot - 1; // decrement reader count
                if (Interlocked.CompareExchange(ref _state, newState, snapshot) == snapshot)
                {
                    return;
                }

                Thread.Yield();
            }
        }

        /// <summary>
        /// Acquires the lock for writing. Blocks until no readers or writer hold the lock.
        /// </summary>
        public void EnterWriteLock()
        {
            while (true)
            {
                // Only succeed when the state is exactly zero (no readers, no writer).
                if (Interlocked.CompareExchange(ref _state, LockConstants.WriterMask, 0) == 0)
                {
                    // Successfully acquired write lock.
                    return;
                }

                // Otherwise, spin.
                Thread.Yield();
            }
        }

        /// <summary>
        /// Releases a previously acquired write lock.
        /// </summary>
        public void ExitWriteLock()
        {
            while (true)
            {
                int snapshot = Volatile.Read(ref _state);
                if ((snapshot & LockConstants.WriterMask) == 0)
                {
                    throw new InvalidOperationException("Attempt to release a write lock that was not held.");
                }

                int newState = snapshot & ~LockConstants.WriterMask; // clear writer flag
                if (Interlocked.CompareExchange(ref _state, newState, snapshot) == snapshot)
                {
                    return;
                }

                Thread.Yield();
            }
        }

        /// <summary>
        /// For diagnostic purposes only: returns the current reader count.
        /// </summary>
        public int CurrentReaderCount => Volatile.Read(ref _state) & LockConstants.ReaderMask;

        /// <summary>
        /// For diagnostic purposes only: returns true if a writer holds the lock.
        /// </summary>
        public bool IsWriterActive => (Volatile.Read(ref _state) & LockConstants.WriterMask) != 0;
    }
}
