using System;

namespace SpatialAudio
{
    /// <summary>
    /// Simple immutable 3‑D vector.
    /// </summary>
    public readonly struct Vector3 : IEquatable<Vector3>
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);

        public float Length()
        {
            return MathF.Sqrt(X * X + Y * Y + Z * Z);
        }

        public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public static Vector3 Zero => new Vector3(0f, 0f, 0f);
    }

    /// <summary>
    /// Core processor that converts a mono signal into a simple stereo spatial effect.
    /// The algorithm uses:
    ///   • Distance attenuation (inverse‑distance law with a floor)
    ///   • Interaural Level Difference (ILD) based on azimuth
    ///   • Interaural Time Difference (ITD) implemented as an integer‑sample delay
    /// No external data (e.g., HRTF tables) is required – the implementation is fully deterministic.
    /// </summary>
    public static class SpatialAudioProcessor
    {
        // Physical constants
        private const float SpeedOfSound = 343.0f;          // meters per second
        private const float EarDistance = 0.18f;           // average distance between ears (meters)

        /// <summary>
        /// Processes a mono buffer into a stereo interleaved buffer.
        /// </summary>
        /// <param name="monoInput">Mono audio samples (float, -1..1).</param>
        /// <param name="position">Source position relative to listener (meters).</param>
        /// <param name="sampleRate">Sampling rate (Hz).</param>
        /// <returns>Stereo interleaved buffer (L,R,L,R,...).</returns>
        public static float[] Process(float[] monoInput, Vector3 position, int sampleRate)
        {
            if (monoInput == null) throw new ArgumentNullException(nameof(monoInput));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));

            // Compute polar coordinates (azimuth, elevation, distance)
            float distance = MathF.Max(position.Length(), 0.001f); // avoid division by zero
            float azimuth = MathF.Atan2(position.X, position.Z); // radians, 0 = front, +right
            // Elevation is ignored for this simple model

            // Distance attenuation (simple inverse distance with a floor to avoid infinite gain)
            float distanceGain = 1.0f / (1.0f + distance);

            // ILD: simple cosine‑based panning law
            // Left gain = cos(azimuth) * distanceGain, Right gain = sin(azimuth) * distanceGain
            // Clamp to [0,1] to avoid negative gains.
            float leftGain = MathF.Max(0.0f, MathF.Cos(azimuth)) * distanceGain;
            float rightGain = MathF.Max(0.0f, MathF.Sin(azimuth)) * distanceGain;

            // ITD: compute delay in samples for each ear.
            // Positive azimuth => source to the right => right ear earlier.
            // Delay = (EarDistance * sin(azimuth)) / SpeedOfSound * sampleRate
            float rawDelaySamples = (EarDistance * MathF.Sin(azimuth)) / SpeedOfSound * sampleRate;
            int leftDelay = rawDelaySamples > 0 ? (int)MathF.Round(rawDelaySamples) : 0;
            int rightDelay = rawDelaySamples < 0 ? (int)MathF.Round(-rawDelaySamples) : 0;

            // Allocate output buffer (interleaved L,R)
            float[] stereoOutput = new float[monoInput.Length * 2];

            // Simple circular buffers for delayed samples (max delay is bounded by ear distance)
            int maxDelay = Math.Max(leftDelay, rightDelay);
            float[] delayBuffer = new float[maxDelay + 1];
            int writeIndex = 0;

            for (int n = 0; n < monoInput.Length; n++)
            {
                // Write current mono sample into delay buffer
                delayBuffer[writeIndex] = monoInput[n];
                // Compute read indices with wrap‑around
                int leftRead = (writeIndex - leftDelay + delayBuffer.Length) % delayBuffer.Length;
                int rightRead = (writeIndex - rightDelay + delayBuffer.Length) % delayBuffer.Length;

                float leftSample = delayBuffer[leftRead] * leftGain;
                float rightSample = delayBuffer[rightRead] * rightGain;

                // Interleave into output
                int outIdx = n * 2;
                stereoOutput[outIdx] = leftSample;
                stereoOutput[outIdx + 1] = rightSample;

                // Advance circular buffer index
                writeIndex = (writeIndex + 1) % delayBuffer.Length;
            }

            return stereoOutput;
        }
    }
}
