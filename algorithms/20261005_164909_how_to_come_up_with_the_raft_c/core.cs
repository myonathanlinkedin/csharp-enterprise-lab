using System;
using System.Collections.Generic;

namespace RaftDemo
{
    public enum NodeState { Follower, Candidate, Leader }

    public record LogEntry(int Term, string Command);

    public record RequestVoteRequest(int Term, int CandidateId, int LastLogIndex, int LastLogTerm);
    public record RequestVoteResponse(int Term, bool VoteGranted);

    public record AppendEntriesRequest(int Term, int LeaderId, int PrevLogIndex, int PrevLogTerm, List<LogEntry> Entries, int LeaderCommit);
    public record AppendEntriesResponse(int Term, bool Success, int MatchIndex);

    public class RaftNode
    {
        public int Id { get; }
        public int CurrentTerm { get; private set; } = 0;
        public int? VotedFor { get; private set; } = null;
        public List<LogEntry> Log { get; } = new();
        public int CommitIndex { get; private set; } = -1;
        public int LastApplied { get; private set; } = -1;
        public NodeState State { get; private set; } = NodeState.Follower;

        private readonly Random _rand = new();
        private int _electionTimeout;
        private int _heartbeatTimeout;
        private int _ticksSinceLastContact = 0;
        private readonly Cluster _cluster;

        public RaftNode(int id, Cluster cluster)
        {
            Id = id;
            _cluster = cluster;
            ResetElectionTimeout();
            _heartbeatTimeout = 50; // fixed heartbeat interval (ticks)
        }

        private void ResetElectionTimeout()
        {
            _electionTimeout = _rand.Next(150, 300);
            _ticksSinceLastContact = 0;
        }

        public void Tick()
        {
            _ticksSinceLastContact++;
            if (State == NodeState.Leader)
            {
                if (_ticksSinceLastContact >= _heartbeatTimeout)
                {
                    SendHeartbeats();
                    _ticksSinceLastContact = 0;
                }
            }
            else
            {
                if (_ticksSinceLastContact >= _electionTimeout)
                {
                    StartElection();
                }
            }
        }

        private void SendHeartbeats()
        {
            foreach (var peer in _cluster.Nodes)
            {
                if (peer.Id == Id) continue;
                var req = new AppendEntriesRequest(
                    Term: CurrentTerm,
                    LeaderId: Id,
                    PrevLogIndex: Log.Count - 1,
                    PrevLogTerm: Log.Count > 0 ? Log[^1].Term : 0,
                    Entries: new List<LogEntry>(),
                    LeaderCommit: CommitIndex);
                _cluster.SendAppendEntries(this, peer, req);
            }
        }

        private void StartElection()
        {
            State = NodeState.Candidate;
            CurrentTerm++;
            VotedFor = Id;
            int votesGranted = 1;
            int needed = (_cluster.Nodes.Count / 2) + 1;

            foreach (var peer in _cluster.Nodes)
            {
                if (peer.Id == Id) continue;
                var lastLogIndex = Log.Count - 1;
                var lastLogTerm = lastLogIndex >= 0 ? Log[lastLogIndex].Term : 0;
                var req = new RequestVoteRequest(CurrentTerm, Id, lastLogIndex, lastLogTerm);
                var resp = _cluster.SendRequestVote(this, peer, req);
                if (resp.VoteGranted) votesGranted++;
                else if (resp.Term > CurrentTerm) { CurrentTerm = resp.Term; State = NodeState.Follower; VotedFor = null; }
            }

            if (State == NodeState.Candidate && votesGranted >= needed)
            {
                BecomeLeader();
            }
            else
            {
                ResetElectionTimeout();
            }
        }

        private void BecomeLeader()
        {
            State = NodeState.Leader;
            // Initialize nextIndex & matchIndex if needed (omitted for brevity)
            SendHeartbeats();
        }

        public RequestVoteResponse OnRequestVote(RequestVoteRequest req)
        {
            if (req.Term < CurrentTerm) return new RequestVoteResponse(CurrentTerm, false);
            if (req.Term > CurrentTerm)
            {
                CurrentTerm = req.Term;
                State = NodeState.Follower;
                VotedFor = null;
            }

            bool upToDate = IsCandidateLogUpToDate(req.LastLogIndex, req.LastLogTerm);
            bool voteGranted = (VotedFor == null || VotedFor == req.CandidateId) && upToDate;
            if (voteGranted) VotedFor = req.CandidateId;
            ResetElectionTimeout();
            return new RequestVoteResponse(CurrentTerm, voteGranted);
        }

        private bool IsCandidateLogUpToDate(int candidateIndex, int candidateTerm)
        {
            int lastIndex = Log.Count - 1;
            int lastTerm = lastIndex >= 0 ? Log[lastIndex].Term : 0;
            if (candidateTerm != lastTerm) return candidateTerm > lastTerm;
            return candidateIndex >= lastIndex;
        }

        public AppendEntriesResponse OnAppendEntries(AppendEntriesRequest req)
        {
            if (req.Term < CurrentTerm) return new AppendEntriesResponse(CurrentTerm, false, -1);
            ResetElectionTimeout();
            if (req.Term > CurrentTerm)
            {
                CurrentTerm = req.Term;
                State = NodeState.Follower;
                VotedFor = null;
            }

            // consistency check
            if (req.PrevLogIndex >= 0)
            {
                if (req.PrevLogIndex >= Log.Count) return new AppendEntriesResponse(CurrentTerm, false, -1);
                if (Log[req.PrevLogIndex].Term != req.PrevLogTerm) return new AppendEntriesResponse(CurrentTerm, false, -1);
            }

            // append new entries
            int index = req.PrevLogIndex + 1;
            foreach (var entry in req.Entries)
            {
                if (index < Log.Count)
                {
                    if (Log[index].Term != entry.Term)
                    {
                        Log.RemoveRange(index, Log.Count - index);
                        Log.Add(entry);
                    }
                }
                else
                {
                    Log.Add(entry);
                }
                index++;
            }

            if (req.LeaderCommit > CommitIndex)
            {
                CommitIndex = Math.Min(req.LeaderCommit, Log.Count - 1);
                ApplyLogEntries();
            }

            return new AppendEntriesResponse(CurrentTerm, true, index - 1);
        }

        private void ApplyLogEntries()
        {
            while (LastApplied < CommitIndex)
            {
                LastApplied++;
                // In real system, apply Log[LastApplied].Command to state machine.
            }
        }

        public bool Propose(string command)
        {
            if (State != NodeState.Leader) return false;
            var entry = new LogEntry(CurrentTerm, command);
            Log.Add(entry);
            // Immediately try to replicate (simplified: send to all)
            foreach (var peer in _cluster.Nodes)
            {
                if (peer.Id == Id) continue;
                var prevIndex = Log.Count - 2;
                var prevTerm = prevIndex >= 0 ? Log[prevIndex].Term : 0;
                var req = new AppendEntriesRequest(CurrentTerm, Id, prevIndex, prevTerm, new List<LogEntry>{ entry }, CommitIndex);
                var resp = _cluster.SendAppendEntries(this, peer, req);
                if (resp.Success) CommitIndex = Log.Count - 1;
            }
            ApplyLogEntries();
            return true;
        }
    }

    public class Cluster
    {
        public List<RaftNode> Nodes { get; } = new();

        public void AddNode(RaftNode node) => Nodes.Add(node);

        public RequestVoteResponse SendRequestVote(RaftNode from, RaftNode to, RequestVoteRequest req)
        {
            return to.OnRequestVote(req);
        }

        public AppendEntriesResponse SendAppendEntries(RaftNode from, RaftNode to, AppendEntriesRequest req)
        {
            return to.OnAppendEntries(req);
        }

        public void TickAll()
        {
            foreach (var n in Nodes) n.Tick();
        }

        public RaftNode? GetLeader()
        {
            foreach (var n in Nodes) if (n.State == NodeState.Leader) return n;
            return null;
        }
    }
}
