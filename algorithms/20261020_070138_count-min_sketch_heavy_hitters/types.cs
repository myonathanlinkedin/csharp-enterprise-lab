using System;
using System.Collections.Generic;

namespace CountMinSketchNS
{
    /// <summary>
    /// A Count‑Min Sketch data structure for frequency estimation of items of type <typeparamref name="T"/>.
    /// The implementation is deterministic, uses only the standard library and provides
    /// Add, Estimate and Heavy‑Hitter extraction operations.
    /// </summary>
    /// <typeparam name="T">The type of the items to be counted. Must be non‑null.</typeparam>
    public sealed class CountMinSketch<T> where T : notnull
    {
        // Number of hash rows (depth) and number of counters per row (width).
        internal readonly int Depth;
        internal readonly int Width;

        // 2‑dimensional counter matrix.
        internal readonly long[,] Table;

        // Per‑row random seeds used to derive independent hash functions.
        private readonly uint[] _seeds;

        // Equality comparer for the generic type.
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Creates a new Count‑Min Sketch.
        /// </summary>
        /// <param name="depth">Number of hash functions (rows). Must be &gt; 0.</param>
        /// <param name="width">Number of counters per row (columns). Must be &gt; 0.</param>
        /// <param name="comparer">Optional equality comparer for <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="depth"/> or <paramref name="width"/> is not positive.</exception>
        public CountMinSketch(int depth, int width, IEqualityComparer<T>? comparer = null)
        {
            if (depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be positive.");
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");

            Depth = depth;
            Width = width;
            Table = new long[Depth, Width];
            _seeds = GenerateSeeds(depth);
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Adds <paramref name="increment"/> occurrences of <paramref name="item"/> to the sketch.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="increment">Number of occurrences to add. Must be non‑negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="increment"/> is negative.</exception>
        public void Add(T item, long increment = 1)
        {
            if (increment < 0)
                throw new ArgumentOutOfRangeException(nameof(increment), "Increment must be non‑negative.");

            // Fast path for zero increment.
            if (increment == 0)
                return;

            int hash = _comparer.GetHashCode(item);
            for (int i = 0; i < Depth; i++)
            {
                int column = ComputeColumn(hash, _seeds[i]);
                Table[i, column] += increment;
            }
        }

        /// <summary>
        /// Estimates the frequency of <paramref name="item"/>. The returned value is an upper bound
        /// on the true count with probability 1.
        /// </summary>
        /// <param name="item">The item to query.</param>
        /// <returns>Estimated frequency (non‑negative).</returns>
        public long Estimate(T item)
        {
            int hash = _comparer.GetHashCode(item);
            long min = long.MaxValue;
            for (int i = 0; i < Depth; i++)
            {
                int column = ComputeColumn(hash, _seeds[i]);
                long value = Table[i, column];
                if (value < min)
                    min = value;
            }
            return min;
        }

        /// <summary>
        /// Merges another sketch into this one. Both sketches must have identical dimensions.
        /// </summary>
        /// <param name="other">The sketch to merge.</param>
        /// <exception cref="ArgumentException">If dimensions differ.</exception>
        public void Merge(CountMinSketch<T> other)
        {
            if (other.Depth != Depth || other.Width != Width)
                throw new ArgumentException("Sketch dimensions must match for merge.");

            for (int i = 0; i < Depth; i++)
                for (int j = 0; j < Width; j++)
                    Table[i, j] += other.Table[i, j];
        }

        /// <summary>
        /// Returns the set of items from <paramref name="candidates"/> whose estimated frequency
        /// is at least <paramref name="threshold"/>.
        /// </summary>
        /// <param name="candidates">Enumerable of items to test.</param>
        /// <param name="threshold">Frequency threshold (inclusive).</param>
        /// <returns>List of heavy‑hitters.</returns>
        public IList<T> GetHeavyHitters(IEnumerable<T> candidates, long threshold)
        {
            if (threshold < 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be non‑negative.");

            var result = new List<T>();
            foreach (var item in candidates)
            {
                if (Estimate(item) >= threshold)
                    result.Add(item);
            }
            return result;
        }

        // --------------------------------------------------------------------
        // Private helpers
        // --------------------------------------------------------------------
        private static uint[] GenerateSeeds(int count)
        {
            // Deterministic pseudo‑random seeds based on a simple linear congruential generator.
            const uint a = 1664525u;
            const uint c = 1013904223u;
            uint[] seeds = new uint[count];
            uint state = 0xBEEFDEADu;
            for (int i = 0; i < count; i++)
            {
                state = a * state + c;
                seeds[i] = state;
            }
            return seeds;
        }

        private int ComputeColumn(int hash, uint seed)
        {
            // Mix the base hash with the per‑row seed and map to [0, Width).
            uint mixed = (uint)hash ^ seed;
            // Use bit‑masking when Width is a power of two for speed; otherwise fallback to modulo.
            return Width.IsPowerOfTwo()
                ? (int)(mixed & (uint)(Width - 1))
                : (int)(mixed % (uint)Width);
        }
    }

    // ------------------------------------------------------------------------
    // Extension helpers (internal to the namespace)
    // ------------------------------------------------------------------------
    internal static class IntExtensions
    {
        /// <summary>
        /// Returns true if the integer is a power of two (including 1).
        /// </summary>
        public static bool IsPowerOfTwo(this int value) =>
            value > 0 && (value & (value - 1)) == 0;
    }
}
