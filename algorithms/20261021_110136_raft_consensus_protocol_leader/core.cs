using System;
using System.Collections.Generic;

namespace RaftConsensus
{
    public enum NodeState
    {
        Follower,
        Candidate,
        Leader
    }

    public class RaftNode
    {
        // Persistent state
        public int Id { get; }
        public int CurrentTerm { get; private set; } = 0;
        public int? VotedFor { get; private set; } = null;

        // Volatile state
        public NodeState State { get; private set; } = NodeState.Follower;
        private int electionTimeout;               // in ticks
        private int elapsedSinceLastContact = 0;   // in ticks
        private readonly Func<int> timeoutGenerator;
        private readonly ICluster cluster;
        private readonly int majority;

        // Candidate state
        private int votesReceived = 0;

        public RaftNode(int id, ICluster cluster, Func<int> timeoutGenerator)
        {
            Id = id;
            this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            this.timeoutGenerator = timeoutGenerator ?? throw new ArgumentNullException(nameof(timeoutGenerator));
            electionTimeout = timeoutGenerator();
            majority = (cluster.NodeCount / 2) + 1;
        }

        // Called each logical tick
        public void Tick()
        {
            elapsedSinceLastContact++;
            if (elapsedSinceLastContact >= electionTimeout)
            {
                StartElection();
            }
        }

        // Leader sends periodic heartbeats
        public void SendHeartbeats()
        {
            if (State != NodeState.Leader) return;
            foreach (var peer in cluster.GetPeers(Id))
            {
                peer.ReceiveAppendEntries(CurrentTerm, Id);
            }
        }

        // RPC: AppendEntries (heartbeat)
        public void ReceiveAppendEntries(int term, int leaderId)
        {
            if (term < CurrentTerm)
            {
                // stale heartbeat, ignore
                return;
            }

            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                VotedFor = null;
            }

            BecomeFollower(term);
            // Reset election timer
            ResetElectionTimer();
        }

        // RPC: RequestVote
        public void ReceiveRequestVote(int term, int candidateId)
        {
            if (term < CurrentTerm)
            {
                // reject
                cluster.SendVoteResponse(candidateId, CurrentTerm, false);
                return;
            }

            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                VotedFor = null;
            }

            bool grant = (!VotedFor.HasValue || VotedFor.Value == candidateId);
            if (grant)
            {
                VotedFor = candidateId;
                ResetElectionTimer();
            }

            cluster.SendVoteResponse(candidateId, CurrentTerm, grant);
        }

        // RPC: Vote response (only meaningful for candidates)
        public void ReceiveVoteResponse(int term, bool voteGranted)
        {
            if (State != NodeState.Candidate) return;

            if (term > CurrentTerm)
            {
                // discovered higher term, step down
                CurrentTerm = term;
                VotedFor = null;
                BecomeFollower(term);
                return;
            }

            if (term < CurrentTerm) return; // stale response

            if (voteGranted)
            {
                votesReceived++;
                if (votesReceived >= majority)
                {
                    BecomeLeader();
                }
            }
        }

        private void StartElection()
        {
            State = NodeState.Candidate;
            CurrentTerm++;
            VotedFor = Id;
            votesReceived = 1; // vote for self
            ResetElectionTimer();

            foreach (var peer in cluster.GetPeers(Id))
            {
                peer.ReceiveRequestVote(CurrentTerm, Id);
            }
        }

        private void BecomeFollower(int term)
        {
            State = NodeState.Follower;
            CurrentTerm = term;
            VotedFor = null;
        }

        private void BecomeLeader()
        {
            State = NodeState.Leader;
            // Upon election: send initial empty AppendEntries (heartbeat)
            SendHeartbeats();
        }

        private void ResetElectionTimer()
        {
            elapsedSinceLastContact = 0;
            electionTimeout = timeoutGenerator();
        }

        // Expose for testing
        public int VotesReceived => votesReceived;
    }

    // Minimal cluster abstraction for unit testing
    public interface ICluster
    {
        int NodeCount { get; }
        IEnumerable<RaftNode> GetPeers(int selfId);
        void SendVoteResponse(int destinationId, int term, bool voteGranted);
    }
}
