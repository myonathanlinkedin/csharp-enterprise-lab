using System;
using System.Collections.Generic;
using GossipFailureDetectorLib;

namespace GossipFailureDetectorApp
{
    internal static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition) throw new InvalidOperationException("Assert.IsTrue failed. " + message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Assert.AreEqual failed. Expected:{expected} Actual:{actual}. {message}");
        }

        public static void Throws<TException>(Action action, string message = "") where TException : Exception
        {
            try
            {
                action();
                throw new InvalidOperationException($"Assert.Throws failed. Expected exception of type {typeof(TException).Name}. {message}");
            }
            catch (TException)
            {
                // Expected
            }
        }
    }

    internal static class Tests
    {
        public static void RunAll()
        {
            TestRegistrationAndHeartbeat();
            TestGossipMerge();
            TestFailureDetection();
            TestUnknownNodeGossip();
            TestNoFalsePositives();
            Console.WriteLine("All tests passed.");
        }

        private static void TestRegistrationAndHeartbeat()
        {
            var now = DateTime.UtcNow;
            var detector = new GossipFailureDetector(TimeSpan.FromSeconds(10));
            detector.RegisterNode("A", now);
            Assert.AreEqual(0L, detector.GetCounter("A"));
            detector.IncrementHeartbeat("A", now.AddSeconds(1));
            Assert.AreEqual(1L, detector.GetCounter("A"));
            Assert.AreEqual(now.AddSeconds(1), detector.GetLastSeen("A"));
        }

        private static void TestGossipMerge()
        {
            var now = DateTime.UtcNow;
            var detector1 = new GossipFailureDetector(TimeSpan.FromSeconds(10));
            var detector2 = new GossipFailureDetector(TimeSpan.FromSeconds(10));

            detector1.RegisterNode("A", now);
            detector2.RegisterNode("B", now);

            detector1.IncrementHeartbeat("A", now.AddSeconds(1)); // A counter =1
            detector2.IncrementHeartbeat("B", now.AddSeconds(2)); // B counter =1

            var gossipFrom1 = detector1.GenerateGossip(); // {A:1}
            var gossipFrom2 = detector2.GenerateGossip(); // {B:1}

            // Exchange gossip
            detector1.ReceiveGossip(gossipFrom2, now.AddSeconds(3));
            detector2.ReceiveGossip(gossipFrom1, now.AddSeconds(3));

            // Verify both detectors now know both nodes
            Assert.AreEqual(1L, detector1.GetCounter("A"));
            Assert.AreEqual(1L, detector1.GetCounter("B"));
            Assert.AreEqual(1L, detector2.GetCounter("A"));
            Assert.AreEqual(1L, detector2.GetCounter("B"));
        }

        private static void TestFailureDetection()
        {
            var now = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(5);
            var detector = new GossipFailureDetector(timeout);
            detector.RegisterNode("A", now);
            detector.RegisterNode("B", now);

            detector.IncrementHeartbeat("A", now.AddSeconds(1)); // A seen at t+1
            // B never updates after registration at t

            var failuresAtT4 = detector.DetectFailures(now.AddSeconds(4));
            Assert.IsTrue(failuresAtT4.Count == 0, "No node should have timed out yet.");

            var failuresAtT7 = detector.DetectFailures(now.AddSeconds(7));
            Assert.IsTrue(failuresAtT7.Count == 1 && failuresAtT7[0] == "B", "Node B should be reported as failed.");
        }

        private static void TestUnknownNodeGossip()
        {
            var now = DateTime.UtcNow;
            var detector = new GossipFailureDetector(TimeSpan.FromSeconds(10));
            detector.RegisterNode("A", now);
            detector.IncrementHeartbeat("A", now.AddSeconds(1));

            var externalGossip = new Dictionary<string, long>
            {
                { "X", 3 },
                { "A", 2 } // higher than local A counter
            };

            detector.ReceiveGossip(externalGossip, now.AddSeconds(2));

            // X should be added
            Assert.AreEqual(3L, detector.GetCounter("X"));
            // A counter should be updated to 2
            Assert.AreEqual(2L, detector.GetCounter("A"));
        }

        private static void TestNoFalsePositives()
        {
            var now = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(3);
            var detector = new GossipFailureDetector(timeout);
            detector.RegisterNode("A", now);
            detector.IncrementHeartbeat("A", now.AddSeconds(1));
            detector.IncrementHeartbeat("A", now.AddSeconds(2));

            // At t+4, last seen is t+2, elapsed =2 < timeout
            var failures = detector.DetectFailures(now.AddSeconds(4));
            Assert.IsTrue(failures.Count == 0, "Node A should not be marked failed.");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Tests.RunAll();
        }
    }
}
