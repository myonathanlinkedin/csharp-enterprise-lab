using System;

namespace ReadWriteLockDemo
{
    internal static class LockConstants
    {
        // Use 30 bits for reader count, 1 bit for writer flag.
        public const int ReaderMask = 0x3FFFFFFF; // lower 30 bits
        public const int WriterMask = 1 << 30;   // bit 30
    }
}
