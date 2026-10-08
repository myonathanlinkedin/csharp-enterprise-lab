using System;

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
        public int Id { get; }
        public NodeState State { get; private set; }
        public int CurrentTerm { get; private set; }
        public int? VotedFor { get; private set; }

        private readonly int electionTimeout;
        private readonly int heartbeatInterval;
        private int elapsed;
        private int votesReceived;

        public RaftNode(int id, int electionTimeout = 5, int heartbeatInterval = 2)
        {
            Id = id;
            State = NodeState.Follower;
            CurrentTerm = 0;
            VotedFor = null;
            this.electionTimeout = electionTimeout;
            this.heartbeatInterval = heartbeatInterval;
            elapsed = 0;
            votesReceived = 0;
        }

        public void Tick()
        {
            elapsed++;
            if (State == NodeState.Follower || State == NodeState.Candidate)
            {
                if (elapsed >= electionTimeout)
                {
                    StartElection();
                }
            }
            else if (State == NodeState.Leader)
            {
                if (elapsed >= heartbeatInterval)
                {
                    SendHeartbeat();
                }
            }
        }

        private void StartElection()
        {
            State = NodeState.Candidate;
            CurrentTerm++;
            VotedFor = Id;
            votesReceived = 1;
            elapsed = 0;
        }

        public void ReceiveVoteResponse(bool voteGranted, int term, int totalNodes)
        {
            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                State = NodeState.Follower;
                VotedFor = null;
                return;
            }
            if (State != NodeState.Candidate) return;
            if (voteGranted)
            {
                votesReceived++;
                if (votesReceived > totalNodes / 2)
                {
                    BecomeLeader();
                }
            }
        }

        public void ReceiveHeartbeat(int term)
        {
            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                State = NodeState.Follower;
                VotedFor = null;
            }
            else if (term == CurrentTerm)
            {
                State = NodeState.Follower;
            }
            elapsed = 0;
        }

        private void SendHeartbeat()
        {
            // In a real implementation, AppendEntries RPCs would be sent.
            // Here we simply reset the heartbeat timer.
            elapsed = 0;
        }

        private void BecomeLeader()
        {
            State = NodeState.Leader;
            elapsed = 0;
        }

        public bool IsLeader => State == NodeState.Leader;
    }
}
