using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumStreaming
{
    /// <summary>
    /// Provides random-order streaming with replenishment and robust lower‑bound estimation.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public sealed class RandomOrderStream<T>
    {
        private readonly List<T> _buffer;
        private readonly Random _rng;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public RandomOrderStream()
        {
            _buffer = new List<T>();
            _rng = new Random();
        }

        /// <summary>
        /// Current number of stored elements.
        /// </summary>
        public int Count => _buffer.Count;

        /// <summary>
        /// Adds a single element to the stream.
        /// </summary>
        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _buffer.Add(item);
        }

        /// <summary>
        /// Adds a collection of elements to the stream.
        /// </summary>
        public void Replenish(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("Null element in replenishment collection.", nameof(items));
                _buffer.Add(item);
            }
        }

        /// <summary>
        /// Returns <paramref name="k"/> distinct random elements without modifying the stream.
        /// If <paramref name="k"/> exceeds the current count, all elements are returned in random order.
        /// </summary>
        public IReadOnlyList<T> Sample(int k)
        {
            if (k < 0) throw new ArgumentOutOfRangeException(nameof(k), "Sample size cannot be negative.");
            int n = _buffer.Count;
            if (n == 0) return Array.Empty<T>();

            // If k >= n, shuffle whole buffer and return.
            if (k >= n)
            {
                var copy = new List<T>(_buffer);
                ShuffleInPlace(copy);
                return copy;
            }

            // Reservoir sampling for k < n.
            var reservoir = new T[k];
            for (int i = 0; i < k; i++) reservoir[i] = _buffer[i];

            for (int i = k; i < n; i++)
            {
                int j = _rng.Next(i + 1);
                if (j < k)
                {
                    reservoir[j] = _buffer[i];
                }
            }

            // Shuffle reservoir to guarantee uniform order.
            var result = reservoir.ToList();
            ShuffleInPlace(result);
            return result;
        }

        /// <summary>
        /// Computes a robust lower bound on the true frequency of an event given an observed count.
        /// Uses Hoeffding's inequality: P[ X - μ ≥ ε ] ≤ exp(-2ε² / n).
        /// Returns μ̂ - ε where ε = sqrt( (ln(1/δ) * n) / (2) ).
        /// </summary>
        /// <param name="observedCount">Observed count of the event.</param>
        /// <param name="confidence">Desired confidence level (0 < confidence < 1).</param>
        /// <returns>Lower bound on the true count.</returns>
        public double ComputeRobustLowerBound(int observedCount, double confidence)
        {
            if (observedCount < 0) throw new ArgumentOutOfRangeException(nameof(observedCount));
            if (confidence <= 0.0 || confidence >= 1.0) throw new ArgumentOutOfRangeException(nameof(confidence));

            int n = _buffer.Count;
            if (n == 0) throw new InvalidOperationException("Cannot compute bound on empty stream.");

            // δ = 1 - confidence
            double delta = 1.0 - confidence;
            double epsilon = Math.Sqrt((Math.Log(1.0 / delta) * n) / (2.0));
            double lower = observedCount - epsilon;
            return lower < 0 ? 0.0 : lower;
        }

        private void ShuffleInPlace(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
