using System;
using System.Collections.Generic;
using RaftConsensus;

namespace RaftTest
{
    // Simple in‑memory cluster implementation
    internal class TestCluster : ICluster
    {
        private readonly Dictionary<int, RaftNode> nodes = new Dictionary<int, RaftNode>();
        public int NodeCount => nodes.Count;

        public void AddNode(RaftNode node) => nodes[node.Id] = node;

        public IEnumerable<RaftNode> GetPeers(int selfId)
        {
            foreach (var kvp in nodes)
            {
                if (kvp.Key != selfId) yield return kvp.Value;
            }
        }

        public void SendVoteResponse(int destinationId, int term, bool voteGranted)
        {
            if (nodes.TryGetValue(destinationId, out var dest))
            {
                dest.ReceiveVoteResponse(term, voteGranted);
            }
        }

        // Helper to advance all nodes by one tick
        public void TickAll()
        {
            foreach (var node in nodes.Values)
                node.Tick();
        }

        // Helper to force all leaders to send heartbeats
        public void HeartbeatAll()
        {
            foreach (var node in nodes.Values)
                node.SendHeartbeats();
        }
    }

    internal static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"Assertion failed: Expected {expected}, Got {actual}. {message}");
        }
    }

    internal class Program
    {
        // Deterministic timeout generator for tests (fixed 3 ticks)
        private static int FixedTimeout() => 3;

        static void Main()
        {
            TestSingleNodeElection();
            TestThreeNodeElection();
            TestHigherTermStepDown();
            TestElectionLossDueToHigherTerm();

            Console.WriteLine("All Raft leader election tests passed.");
        }

        private static void TestSingleNodeElection()
        {
            var cluster = new TestCluster();
            var node = new RaftNode(0, cluster, FixedTimeout);
            cluster.AddNode(node);

            // Advance ticks until election occurs
            for (int i = 0; i < 5; i++) cluster.TickAll();

            Assert.AreEqual(NodeState.Leader, node.State, "Single node should become leader.");
            Assert.AreEqual(1, node.CurrentTerm, "Term should be 1 after election.");
        }

        private static void TestThreeNodeElection()
        {
            var cluster = new TestCluster();
            var n0 = new RaftNode(0, cluster, FixedTimeout);
            var n1 = new RaftNode(1, cluster, FixedTimeout);
            var n2 = new RaftNode(2, cluster, FixedTimeout);
            cluster.AddNode(n0);
            cluster.AddNode(n1);
            cluster.AddNode(n2);

            // Let node 0 timeout first
            for (int i = 0; i < 3; i++) cluster.TickAll();

            // Node 0 should have started election and become leader after receiving votes
            Assert.AreEqual(NodeState.Leader, n0.State, "Node 0 should be leader after election.");
            Assert.AreEqual(1, n0.CurrentTerm, "Leader term should be 1.");
            Assert.AreEqual(NodeState.Follower, n1.State, "Other nodes should be followers.");
            Assert.AreEqual(NodeState.Follower, n2.State, "Other nodes should be followers.");
        }

        private static void TestHigherTermStepDown()
        {
            var cluster = new TestCluster();
            var n0 = new RaftNode(0, cluster, FixedTimeout);
            var n1 = new RaftNode(1, cluster, FixedTimeout);
            cluster.AddNode(n0);
            cluster.AddNode(n1);

            // Force n0 to become leader
            for (int i = 0; i < 3; i++) cluster.TickAll();
            Assert.AreEqual(NodeState.Leader, n0.State, "n0 should be leader.");

            // n1 receives AppendEntries with higher term
            n1.ReceiveAppendEntries(term: 5, leaderId: 1);
            Assert.AreEqual(NodeState.Follower, n1.State, "n1 should be follower after higher term.");
            Assert.AreEqual(5, n1.CurrentTerm, "n1 term should update to higher term.");
        }

        private static void TestElectionLossDueToHigherTerm()
        {
            var cluster = new TestCluster();
            var n0 = new RaftNode(0, cluster, FixedTimeout);
            var n1 = new RaftNode(1, cluster, FixedTimeout);
            var n2 = new RaftNode(2, cluster, FixedTimeout);
            cluster.AddNode(n0);
            cluster.AddNode(n1);
            cluster.AddNode(n2);

            // Advance time so both n0 and n1 start election on same tick
            for (int i = 0; i < 3; i++) cluster.TickAll();

            // At this point both n0 and n1 are candidates; they will exchange votes.
            // n2 will grant vote to the first request it receives (deterministic order of iteration).
            // Ensure that at least one candidate steps down due to higher term.
            // Run additional ticks to allow vote processing.
            cluster.TickAll();

            // Exactly one node should be leader, the other candidate must have stepped down.
            int leaders = 0;
            foreach (var node in new[] { n0, n1, n2 })
                if (node.State == NodeState.Leader) leaders++;

            Assert.AreEqual(1, leaders, "Exactly one leader must emerge.");
        }
    }
}
