using System;
using System.Collections.Generic;
using GossipDetector;

class Program
{
    static void Main()
    {
        RunAllTests();
        Console.WriteLine("All tests passed.");
    }

    static void RunAllTests()
    {
        TestRegistration();
        TestHeartbeatIncrement();
        TestGossipPropagation();
        TestFailureDetection();
        TestNoFalsePositive();
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {message}");
    }

    static void TestRegistration()
    {
        var detector = new GossipFailureDetector(failureTimeoutSeconds: 5.0);
        detector.RegisterNode("A");
        detector.RegisterNode("B");

        Assert(detector.GetCounter("A") == 0, "Node A counter should start at 0.");
        Assert(detector.GetCounter("B") == 0, "Node B counter should start at 0.");
    }

    static void TestHeartbeatIncrement()
    {
        var detector = new GossipFailureDetector(5.0);
        detector.RegisterNode("A");
        detector.IncrementHeartbeat("A");
        detector.IncrementHeartbeat("A");

        Assert(detector.GetCounter("A") == 2, "Node A counter should be 2 after two increments.");
    }

    static void TestGossipPropagation()
    {
        var detector = new GossipFailureDetector(5.0);
        detector.RegisterNode("A");
        detector.RegisterNode("B");

        detector.IncrementHeartbeat("A"); // A: counter 1
        detector.Gossip("A", "B");        // B learns A's counter

        Assert(detector.GetCounter("A") == 1, "A's own counter unchanged.");
        // B's view of A is internal; we verify via failure detection (no suspect)
        var suspectsB = detector.SuspectFailedNodes("B");
        Assert(suspectsB.Count == 0, "B should not suspect A immediately after gossip.");
    }

    static void TestFailureDetection()
    {
        var detector = new GossipFailureDetector(failureTimeoutSeconds: 3.0);
        detector.RegisterNode("A");
        detector.RegisterNode("B");

        detector.IncrementHeartbeat("A"); // A: counter 1
        detector.Gossip("A", "B");        // B receives fresh heartbeat

        detector.Tick(4.0);               // advance time beyond timeout

        var suspectsB = detector.SuspectFailedNodes("B");
        Assert(suspectsB.Count == 1 && suspectsB[0] == "A", "B should suspect A after timeout.");
    }

    static void TestNoFalsePositive()
    {
        var detector = new GossipFailureDetector(failureTimeoutSeconds: 5.0);
        detector.RegisterNode("A");
        detector.RegisterNode("B");
        detector.RegisterNode("C");

        detector.IncrementHeartbeat("A");
        detector.Gossip("A", "B");
        detector.Tick(2.0);
        detector.Gossip("B", "C"); // C learns about A via B before timeout

        var suspectsC = detector.SuspectFailedNodes("C");
        Assert(suspectsC.Count == 0, "C should not suspect A because gossip arrived before timeout.");

        detector.Tick(4.0); // total elapsed 6 seconds since A's last heartbeat
        suspectsC = detector.SuspectFailedNodes("C");
        Assert(suspectsC.Count == 1 && suspectsC[0] == "A", "C should now suspect A after timeout.");
    }
}
