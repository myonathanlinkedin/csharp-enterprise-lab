using System;

namespace PerseusDemo
{
    /// <summary>
    /// Simple fail‑slow detector based on an exponential moving average (EWMA)
    /// and variance. An alert is raised when a configurable number of
    /// consecutive latency samples exceed the dynamic threshold
    /// (average + k·standard‑deviation).
    /// </summary>
    public sealed class PerseusDetector
    {
        private readonly double _alpha;
        private readonly int _consecutiveThreshold;
        private readonly double _sigmaMultiplier;

        private double _ewma;
        private double _ewmaVar;
        private int _consecutiveHigh;

        /// <summary>
        /// Creates a new detector.
        /// </summary>
        /// <param name="alpha">Smoothing factor for EWMA (0 &lt; alpha ≤ 1).</param>
        /// <param name="consecutiveThreshold">How many consecutive high samples trigger detection.</param>
        /// <param name="sigmaMultiplier">Multiplier for standard deviation to compute the dynamic threshold.</param>
        public PerseusDetector(double alpha = 0.2, int consecutiveThreshold = 3, double sigmaMultiplier = 3.0)
        {
            if (alpha <= 0.0 || alpha > 1.0) throw new ArgumentOutOfRangeException(nameof(alpha));
            if (consecutiveThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(consecutiveThreshold));
            if (sigmaMultiplier <= 0.0) throw new ArgumentOutOfRangeException(nameof(sigmaMultiplier));

            _alpha = alpha;
            _consecutiveThreshold = consecutiveThreshold;
            _sigmaMultiplier = sigmaMultiplier;
            _ewma = 0.0;
            _ewmaVar = 0.0;
            _consecutiveHigh = 0;
        }

        /// <summary>
        /// Adds a new latency sample (in milliseconds) to the detector.
        /// </summary>
        public void AddSample(double latencyMs)
        {
            if (latencyMs < 0.0) throw new ArgumentOutOfRangeException(nameof(latencyMs));

            if (_ewma == 0.0)
            {
                // First sample seeds the EWMA.
                _ewma = latencyMs;
                _ewmaVar = 0.0;
                _consecutiveHigh = 0;
                return;
            }

            double diff = latencyMs - _ewma;
            // Update EWMA.
            _ewma += _alpha * diff;
            // Update EWMA variance using the exponential moving variance formula.
            _ewmaVar = (1 - _alpha) * (_ewmaVar + _alpha * diff * diff);

            double stdDev = Math.Sqrt(_ewmaVar);
            double threshold = _ewma + _sigmaMultiplier * stdDev;

            if (latencyMs > threshold)
                _consecutiveHigh++;
            else
                _consecutiveHigh = 0;
        }

        /// <summary>
        /// Returns true if a fail‑slow condition has been detected.
        /// </summary>
        public bool IsFailSlowDetected()
        {
            return _consecutiveHigh >= _consecutiveThreshold;
        }

        /// <summary>
        /// Resets the detector to its initial state.
        /// </summary>
        public void Reset()
        {
            _ewma = 0.0;
            _ewmaVar = 0.0;
            _consecutiveHigh = 0;
        }

        /// <summary>
        /// Current EWMA of latency samples.
        /// </summary>
        public double CurrentAverage => _ewma;

        /// <summary>
        /// Current estimated standard deviation.
        /// </summary>
        public double CurrentStdDev => Math.Sqrt(_ewmaVar);
    }
}
