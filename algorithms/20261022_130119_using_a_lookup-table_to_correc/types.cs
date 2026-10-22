using System;

namespace LookupCorrectionDemo
{
    /// <summary>
    /// Immutable representation of a lookup table for a monotonic function.
    /// </summary>
    public sealed class LookupTable
    {
        /// <summary>
        /// Minimum input value covered by the table (inclusive).
        /// </summary>
        public double Min { get; }

        /// <summary>
        /// Maximum input value covered by the table (inclusive).
        /// </summary>
        public double Max { get; }

        /// <summary>
        /// Uniform step between successive entries.
        /// </summary>
        public double Step { get; }

        /// <summary>
        /// Pre‑computed exact values of the target function at each sampled point.
        /// </summary>
        public double[] Values { get; }

        /// <summary>
        /// Number of entries in the table.
        /// </summary>
        public int Count => Values.Length;

        public LookupTable(double min, double max, double step, double[] values)
        {
            if (step <= 0) throw new ArgumentException("Step must be positive.", nameof(step));
            if (max <= min) throw new ArgumentException("Max must be greater than Min.", nameof(max));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length < 2) throw new ArgumentException("Table must contain at least two entries.", nameof(values));

            Min = min;
            Max = max;
            Step = step;
            Values = values;
        }

        /// <summary>
        /// Retrieves the exact function value for the sampled point at the given index.
        /// </summary>
        public double this[int index] => Values[index];
    }
}
