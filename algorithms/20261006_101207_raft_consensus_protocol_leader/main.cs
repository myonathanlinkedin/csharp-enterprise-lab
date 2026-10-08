using System;
using RaftConsensus;

namespace RaftConsensusTest
{
    public static class Assert
    {
        public static void Equal<T>(T expected, T actual, string message = "")
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception($"Assertion Failed: Expected {expected}, Actual {actual}. {message}");
            }
        }

        public static void True(bool condition, string message = "")
        {
            if (!condition)
            {
                throw new Exception($"Assertion Failed: Condition is false. {message}");
            }
        }
    }

    public static class TestRunner
    {
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;
            var tests = new Action[]
            {
                TestFollowerTimeoutStartsElection,
                TestCandidateWinsElection,
                TestHeartbeatResetsTimer,
                TestHigherTermFollower,
                TestElectionTimeoutResetOnHeartbeat
            };

            foreach (var test in tests)
            {
                try
                {
                    test();
                    Console.WriteLine($"{test.Method.Name}: PASSED");
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{test.Method.Name}: FAILED - {ex.Message}");
                    failed++;
                }
            }

            Console.WriteLine($"\nTotal: {tests.Length}, Passed: {passed}, Failed: {failed}");
        }

        private static void TestFollowerTimeoutStartsElection()
        {
            var node = new RaftNode(id: 1, electionTimeout: 3);
            node.Tick(); // 1
            node.Tick(); // 2
            Assert.Equal(NodeState.Follower, node.State, "Should still be follower");
            node.Tick(); // 3 -> timeout
            Assert.Equal(NodeState.Candidate, node.State, "Should start election");
        }

        private static void TestCandidateWinsElection()
        {
            var node = new RaftNode(id: 2, electionTimeout: 5);
            node.Tick(); // trigger election
            node.Tick(); // 1
            node.Tick(); // 2
            node.Tick(); // 3
            node.Tick(); // 4
            node.Tick(); // 5 -> election started
            // Simulate votes from 3 other nodes (total 4 nodes)
            node.ReceiveVoteResponse(true, node.CurrentTerm, totalNodes: 4); // self vote already counted
            node.ReceiveVoteResponse(true, node.CurrentTerm, totalNodes: 4);
            node.ReceiveVoteResponse(true, node.CurrentTerm, totalNodes: 4);
            Assert.True(node.IsLeader, "Node should become leader after majority");
        }

        private static void TestHeartbeatResetsTimer()
        {
            var node = new RaftNode(id: 3, electionTimeout: 5, heartbeatInterval: 2);
            // Make node leader
            node.ReceiveHeartbeat(node.CurrentTerm + 1); // force follower to new term
            node.Tick(); // 1
            node.Tick(); // 2 -> heartbeat sent
            // After heartbeat, elapsed should be reset
            Assert.Equal(0, node.GetElapsed(), "Elapsed should reset after heartbeat");
        }

        private static void TestHigherTermFollower()
        {
            var node = new RaftNode(id: 4);
            node.ReceiveHeartbeat(5);
            Assert.Equal(5, node.CurrentTerm, "Term should update to higher term");
            Assert.Equal(NodeState.Follower, node.State, "State should remain follower");
        }

        private static void TestElectionTimeoutResetOnHeartbeat()
        {
            var node = new RaftNode(id: 5, electionTimeout: 4);
            node.Tick(); // 1
            node.ReceiveHeartbeat(1); // reset timer
            node.Tick(); // 1
            node.Tick(); // 2
            node.Tick(); // 3
            node.Tick(); // 4 -> should still be follower, not candidate
            Assert.Equal(NodeState.Follower, node.State, "Should not start election after heartbeat");
        }
    }

    public static class RaftNodeExtensions
    {
        public static int GetElapsed(this RaftNode node)
        {
            var type = typeof(RaftNode);
            var field = type.GetField("elapsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)field.GetValue(node);
        }
    }

    public class Program
    {
        public static void Main()
        {
            TestRunner.RunAll();
        }
    }
}
