using System;
using SpatialAudio;

namespace SpatialAudioTest
{
    internal static class TestRunner
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Assertion failed: " + message);
        }

        private static void AssertApproximatelyEqual(float expected, float actual, float tolerance, string message)
        {
            if (MathF.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException($"Assertion failed: {message} Expected {expected}, got {actual}");
        }

        public static void RunAll()
        {
            TestFrontCenter();
            TestRight90Degrees();
            TestLeft90Degrees();
            TestDistanceAttenuation();
            Console.WriteLine("All tests passed.");
        }

        // Helper to generate a constant mono buffer
        private static float[] GenerateConstant(int length, float value)
        {
            float[] buf = new float[length];
            for (int i = 0; i < length; i++) buf[i] = value;
            return buf;
        }

        private static void TestFrontCenter()
        {
            // Source directly in front (azimuth = 0), distance = 1m
            var position = new Vector3(0f, 0f, 1f);
            float[] mono = GenerateConstant(10, 0.5f);
            float[] stereo = SpatialAudioProcessor.Process(mono, position, 48000);

            // Expected: left and right gains equal, no delay
            float expectedGain = 0.5f / (1.0f + 1.0f); // distanceGain = 0.5
            for (int i = 0; i < mono.Length; i++)
            {
                int idx = i * 2;
                AssertApproximatelyEqual(expectedGain, stereo[idx], 1e-6f, "FrontCenter left gain");
                AssertApproximatelyEqual(expectedGain, stereo[idx + 1], 1e-6f, "FrontCenter right gain");
            }
        }

        private static void TestRight90Degrees()
        {
            // Source directly to the right (azimuth = +π/2), distance = 1m
            var position = new Vector3(1f, 0f, 0f);
            float[] mono = GenerateConstant(20, 1.0f);
            float[] stereo = SpatialAudioProcessor.Process(mono, position, 48000);

            // Expected: left ear delayed, right ear earlier, left gain ~0, right gain ~distanceGain
            float distanceGain = 1.0f / (1.0f + 1.0f);
            float expectedLeftGain = 0.0f; // cos(π/2) = 0
            float expectedRightGain = distanceGain; // sin(π/2) = 1

            // Compute expected integer delay (samples)
            float rawDelay = (0.18f * MathF.Sin(MathF.PI / 2)) / 343.0f * 48000;
            int expectedLeftDelay = (int)MathF.Round(rawDelay); // left ear later
            int expectedRightDelay = 0;

            // Verify first few samples where delay hasn't filled yet
            for (int i = 0; i < expectedLeftDelay; i++)
            {
                int idx = i * 2;
                AssertApproximatelyEqual(0.0f, stereo[idx], 1e-6f, "Right90 left early sample should be zero");
                AssertApproximatelyEqual(0.0f, stereo[idx + 1], 1e-6f, "Right90 right early sample should be zero");
            }

            // Verify a sample after delay is filled
            int sampleIdx = expectedLeftDelay;
            int outIdx = sampleIdx * 2;
            AssertApproximatelyEqual(0.0f, stereo[outIdx], 1e-6f, "Right90 left gain zero after delay");
            AssertApproximatelyEqual(expectedRightGain, stereo[outIdx + 1], 1e-6f, "Right90 right gain after delay");
        }

        private static void TestLeft90Degrees()
        {
            // Source directly to the left (azimuth = -π/2), distance = 2m
            var position = new Vector3(-2f, 0f, 0f);
            float[] mono = GenerateConstant(30, 0.8f);
            float[] stereo = SpatialAudioProcessor.Process(mono, position, 48000);

            float distanceGain = 0.8f / (1.0f + 2.0f); // distance = 2
            float expectedLeftGain = distanceGain; // sin(-π/2) = -1 => rightDelay = 0, leftDelay >0
            float expectedRightGain = 0.0f; // cos(-π/2) = 0

            // Expected delay for right ear (ear later) because source left => right ear delayed
            float rawDelay = (0.18f * MathF.Sin(-MathF.PI / 2)) / 343.0f * 48000;
            int expectedRightDelay = (int)MathF.Round(-rawDelay); // positive delay for right ear
            int expectedLeftDelay = 0;

            // Verify early samples where right ear hasn't received delayed signal yet
            for (int i = 0; i < expectedRightDelay; i++)
            {
                int idx = i * 2;
                AssertApproximatelyEqual(0.0f, stereo[idx], 1e-6f, "Left90 left early zero");
                AssertApproximatelyEqual(0.0f, stereo[idx + 1], 1e-6f, "Left90 right early zero");
            }

            // Verify a sample after delay is filled
            int sampleIdx = expectedRightDelay;
            int outIdx = sampleIdx * 2;
            AssertApproximatelyEqual(expectedLeftGain, stereo[outIdx], 1e-6f, "Left90 left gain after delay");
            AssertApproximatelyEqual(0.0f, stereo[outIdx + 1], 1e-6f, "Left90 right gain zero after delay");
        }

        private static void TestDistanceAttenuation()
        {
            // Same azimuth (front) but varying distance
            var posNear = new Vector3(0f, 0f, 0.5f);
            var posFar = new Vector3(0f, 0f, 5f);
            float[] mono = GenerateConstant(5, 1.0f);
            float[] near = SpatialAudioProcessor.Process(mono, posNear, 48000);
            float[] far = SpatialAudioProcessor.Process(mono, posFar, 48000);

            float gainNear = 1.0f / (1.0f + 0.5f);
            float gainFar = 1.0f / (1.0f + 5.0f);

            for (int i = 0; i < mono.Length; i++)
            {
                int idx = i * 2;
                AssertApproximatelyEqual(gainNear, near[idx], 1e-6f, "Near left gain");
                AssertApproximatelyEqual(gainNear, near[idx + 1], 1e-6f, "Near right gain");
                AssertApproximatelyEqual(gainFar, far[idx], 1e-6f, "Far left gain");
                AssertApproximatelyEqual(gainFar, far[idx + 1], 1e-6f, "Far right gain");
            }
        }
    }

    internal class Program
    {
        private static void Main()
        {
            TestRunner.RunAll();
        }
    }
}
