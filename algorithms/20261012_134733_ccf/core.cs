using System;

namespace CcfImplementation
{
    /// <summary>
    /// A production‑grade implementation of a Fenwick Tree (Binary Indexed Tree),
    /// also known as a Cumulative Count/Frequency (CCF) data structure.
    /// Supports point updates and prefix‑sum queries in O(log n) time,
    /// with O(n) construction.
    /// </summary>
    public sealed class Ccf
    {
        // Internal 1‑based array; index 0 is unused.
        private readonly long[] _tree;
        private readonly int _size;

        /// <summary>
        /// Gets the number of elements the structure can hold.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Constructs an empty CCF of the given size (all zeros).
        /// </summary>
        /// <param name="size">Number of elements (must be &gt; 0).</param>
        public Ccf(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");
            _size = size;
            _tree = new long[size + 1];
        }

        /// <summary>
        /// Builds a CCF from an initial array of values.
        /// </summary>
        /// <param name="values">Zero‑based array of initial values.</param>
        /// <returns>A CCF containing the supplied values.</returns>
        public static Ccf FromArray(long[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var ccf = new Ccf(values.Length);
            for (int i = 0; i < values.Length; i++)
                ccf.Add(i, values[i]); // point update
            return ccf;
        }

        /// <summary>
        /// Adds <paramref name="delta"/> to the element at zero‑based <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Zero‑based index (0 ≤ index &lt; Size).</param>
        /// <param name="delta">Value to add (may be negative).</param>
        public void Add(int index, long delta)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));
            // Convert to 1‑based index for internal tree.
            int i = index + 1;
            while (i <= _size)
            {
                _tree[i] += delta;
                i += i & -i; // climb to next responsible node
            }
        }

        /// <summary>
        /// Returns the prefix sum of elements in the range [0, index].
        /// </summary>
        /// <param name="index">Zero‑based inclusive upper bound.</param>
        /// <returns>Sum of values[0..index].</returns>
        public long PrefixSum(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));
            long sum = 0;
            int i = index + 1;
            while (i > 0)
            {
                sum += _tree[i];
                i -= i & -i; // move to parent
            }
            return sum;
        }

        /// <summary>
        /// Returns the sum of elements in the inclusive range [left, right].
        /// </summary>
        /// <param name="left">Zero‑based start index (0 ≤ left ≤ right).</param>
        /// <param name="right">Zero‑based end index (right &lt; Size).</param>
        /// <returns>Sum of values[left..right].</returns>
        public long RangeSum(int left, int right)
        {
            if (left < 0 || right < 0 || left >= _size || right >= _size)
                throw new ArgumentOutOfRangeException("Indices must be within bounds.");
            if (left > right)
                throw new ArgumentException("Left index cannot be greater than right index.");
            return PrefixSum(right) - (left == 0 ? 0 : PrefixSum(left - 1));
        }

        /// <summary>
        /// Retrieves the current value stored at <paramref name="index"/>.
        /// This operation is O(log n) by computing the difference of two prefix sums.
        /// </summary>
        /// <param name="index">Zero‑based index.</param>
        /// <returns>Current value at the index.</returns>
        public long Get(int index)
        {
            return RangeSum(index, index);
        }
    }
}
