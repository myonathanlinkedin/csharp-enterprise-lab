using System;

namespace LookupCorrectionDemo
{
    /// <summary>
    /// Provides a fast square‑root approximation using a lookup table and linear interpolation.
    /// The algorithm is deterministic, pure, and requires no external resources.
    /// </summary>
    public sealed class SqrtLookupCorrector
    {
        private readonly LookupTable _table;

        /// <summary>
        /// Constructs a corrector for inputs in the closed interval [0, max].
        /// </summary>
        /// <param name="max">Maximum input value (inclusive) for which the table is valid.</param>
        /// <param name="step">Uniform step size between sampled points.</param>
        public SqrtLookupCorrector(double max = 100.0, double step = 0.5)
        {
            if (max <= 0) throw new ArgumentException("Maximum must be positive.", nameof(max));
            if (step <= 0) throw new ArgumentException("Step must be positive.", nameof(step));

            _table = BuildLookupTable(0.0, max, step);
        }

        /// <summary>
        /// Approximates sqrt(x) for 0 ≤ x ≤ Max using table lookup + linear interpolation.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for out‑of‑range inputs.
        /// </summary>
        public double Sqrt(double x)
        {
            if (x < _table.Min || x > _table.Max)
                throw new ArgumentOutOfRangeException(nameof(x), $"Input must be in [{_table.Min}, {_table.Max}].");

            // Exact match case (rare but cheap to check)
            if (x == _table.Min) return _table[0];
            if (x == _table.Max) return _table[_table.Count - 1];

            // Locate the interval [x0, x1] that brackets x.
            int lowerIndex = (int)Math.Floor((x - _table.Min) / _table.Step);
            // Guard against rounding errors at the upper bound.
            if (lowerIndex >= _table.Count - 1) lowerIndex = _table.Count - 2;

            double x0 = _table.Min + lowerIndex * _table.Step;
            double x1 = x0 + _table.Step;

            double y0 = _table[lowerIndex];
            double y1 = _table[lowerIndex + 1];

            // Linear interpolation: y = y0 + (y1 - y0) * ((x - x0) / (x1 - x0))
            double t = (x - x0) / (x1 - x0);
            double approx = y0 + (y1 - y0) * t;

            // The interpolation already yields a high‑quality approximation.
            // For extra precision we perform a single Newton‑Raphson refinement step:
            //   sqrt(x) ≈ approx - (approx^2 - x) / (2 * approx)
            // This step is safe because approx > 0 for x > 0.
            if (approx > 0)
            {
                double correction = (approx * approx - x) / (2.0 * approx);
                approx -= correction;
            }

            return approx;
        }

        /// <summary>
        /// Builds a lookup table for sqrt on the interval [min, max] with the given step.
        /// </summary>
        private static LookupTable BuildLookupTable(double min, double max, double step)
        {
            int count = (int)Math.Floor((max - min) / step) + 1;
            double[] values = new double[count];
            for (int i = 0; i < count; i++)
            {
                double x = min + i * step;
                values[i] = Math.Sqrt(x);
            }
            return new LookupTable(min, max, step, values);
        }
    }
}
