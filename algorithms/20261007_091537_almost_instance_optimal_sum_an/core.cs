using System;
using System.Collections.Generic;

namespace WeightedSampling
{
    /// <summary>
    /// Weighted reservoir sampler that keeps a fixed-size sample of items drawn
    /// with probability proportional to their weight. After the stream ends,
    /// the sampler can provide unbiased Horvitz‑Thompson estimates of the
    /// weighted sum and arbitrary moments of the values.
    /// </summary>
    public sealed class WeightedSampler
    {
        private readonly int _capacity;
        private readonly Random _rand;
        private readonly List<SampleItem> _sample;
        private double _totalWeight; // sum of all weights seen

        private struct SampleItem
        {
            public double Weight;
            public double Value;
            public double Priority; // exponential key = -ln(U)/weight
        }

        /// <summary>
        /// Creates a sampler with the given sample capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items kept in the reservoir.</param>
        /// <param name="seed">Optional random seed for reproducibility.</param>
        public WeightedSampler(int capacity, int? seed = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _rand = seed.HasValue ? new Random(seed.Value) : new Random();
            _sample = new List<SampleItem>(capacity);
            _totalWeight = 0.0;
        }

        /// <summary>
        /// Adds an item with the specified weight and value to the stream.
        /// </summary>
        /// <param name="weight">Non‑negative weight of the item (zero weight items are ignored).</param>
        /// <param name="value">The numeric value associated with the item.</param>
        public void Add(double weight, double value)
        {
            if (weight < 0.0) throw new ArgumentOutOfRangeException(nameof(weight));
            if (weight == 0.0) return; // zero‑weight items never affect estimates

            _totalWeight += weight;

            // Generate exponential priority: E = -ln(U)/weight, U~Uniform(0,1)
            double u = _rand.NextDouble();
            // Guard against u == 0 (extremely unlikely) to avoid division by zero
            if (u <= double.Epsilon) u = double.Epsilon;
            double priority = -Math.Log(u) / weight;

            if (_sample.Count < _capacity)
            {
                _sample.Add(new SampleItem { Weight = weight, Value = value, Priority = priority });
                if (_sample.Count == _capacity)
                {
                    // Build a max‑heap based on priority (largest priority = smallest key)
                    _sample.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }
                return;
            }

            // The heap is stored in descending order of priority (largest first)
            // The item with the largest priority is the least likely to stay.
            if (priority < _sample[0].Priority)
            {
                // Replace the worst item and restore heap order
                _sample[0] = new SampleItem { Weight = weight, Value = value, Priority = priority };
                // Re‑heapify: bubble down the new root
                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    int right = left + 1;
                    int smallest = i;

                    if (left < _capacity && _sample[left].Priority > _sample[smallest].Priority)
                        smallest = left;
                    if (right < _capacity && _sample[right].Priority > _sample[smallest].Priority)
                        smallest = right;

                    if (smallest == i) break;

                    var tmp = _sample[i];
                    _sample[i] = _sample[smallest];
                    _sample[smallest] = tmp;
                    i = smallest;
                }
            }
        }

        /// <summary>
        /// Returns the Horvitz‑Thompson unbiased estimate of the weighted sum of values.
        /// </summary>
        public double EstimateSum()
        {
            if (_totalWeight == 0.0) return 0.0;
            double threshold = _capacity / _totalWeight; // scaling constant C
            double estimate = 0.0;
            foreach (var item in _sample)
            {
                double inclusionProb = Math.Min(1.0, item.Weight * threshold);
                // Guard against division by zero (should not happen)
                if (inclusionProb <= 0.0) continue;
                estimate += item.Value / inclusionProb;
            }
            return estimate;
        }

        /// <summary>
        /// Returns the Horvitz‑Thompson unbiased estimate of the k‑th moment
        /// Σ (value^k) weighted by the item weights.
        /// </summary>
        /// <param name="k">Non‑negative integer exponent.</param>
        public double EstimateMoment(int k)
        {
            if (k < 0) throw new ArgumentOutOfRangeException(nameof(k));
            if (_totalWeight == 0.0) return 0.0;
            double threshold = _capacity / _totalWeight;
            double estimate = 0.0;
            foreach (var item in _sample)
            {
                double inclusionProb = Math.Min(1.0, item.Weight * threshold);
                if (inclusionProb <= 0.0) continue;
                double powered = Math.Pow(item.Value, k);
                estimate += powered / inclusionProb;
            }
            return estimate;
        }

        /// <summary>
        /// Returns the total weight of all items seen so far.
        /// </summary>
        public double TotalWeight => _totalWeight;

        /// <summary>
        /// Returns the number of items currently stored in the reservoir.
        /// </summary>
        public int SampleSize => _sample.Count;
    }
}
