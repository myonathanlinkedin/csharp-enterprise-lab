using System;
using System.Collections.Generic;
using System.Linq;

namespace TwoPhaseCommit
{
    // Concrete implementation of a participant.
    // The participant can be configured to vote Commit or Abort.
    public sealed class Participant : IParticipant
    {
        private readonly Vote _configuredVote;
        private ParticipantState _state;

        public Guid Id { get; }

        public ParticipantState State => _state;

        public Participant(Vote configuredVote)
        {
            Id = Guid.NewGuid();
            _configuredVote = configuredVote;
            _state = ParticipantState.Init;
        }

        public Vote Prepare()
        {
            if (_state != ParticipantState.Init)
                throw new InvalidOperationException($"Prepare called in invalid state {_state}.");

            // Transition to Prepared if we intend to commit, otherwise stay Init and abort later.
            if (_configuredVote == Vote.Commit)
                _state = ParticipantState.Prepared;
            else
                _state = ParticipantState.Aborted; // Immediate abort if we know we cannot commit.

            return _configuredVote;
        }

        public void ReceiveDecision(Decision decision)
        {
            // Decision may be received multiple times; the state machine must be idempotent.
            switch (decision)
            {
                case Decision.Commit:
                    if (_state == ParticipantState.Prepared)
                        _state = ParticipantState.Committed;
                    else if (_state == ParticipantState.Aborted)
                        // Already aborted; ignore commit (protocol violation) but keep abort.
                        _state = ParticipantState.Aborted;
                    break;

                case Decision.Abort:
                    // Abort overrides any prior state except Committed (which should never happen).
                    if (_state != ParticipantState.Committed)
                        _state = ParticipantState.Aborted;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown decision {decision}.");
            }
        }
    }

    // Coordinator that drives the two‑phase commit protocol.
    public sealed class Coordinator
    {
        private readonly List<IParticipant> _participants;
        private CoordinatorState _state;
        private Decision? _finalDecision; // Nullable until decided

        public CoordinatorState State => _state;
        public Decision? FinalDecision => _finalDecision;

        public Coordinator(IEnumerable<IParticipant> participants)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            _participants = participants.ToList();
            if (_participants.Count == 0) throw new ArgumentException("At least one participant required.", nameof(participants));
            _state = CoordinatorState.Init;
        }

        // Executes a full two‑phase commit cycle.
        // Returns the final decision (Commit or Abort).
        public Decision Execute()
        {
            if (_state != CoordinatorState.Init)
                throw new InvalidOperationException($"Execute called in invalid state {_state}.");

            _state = CoordinatorState.Collecting;

            // Phase 1 – Prepare
            var votes = new List<Vote>();
            foreach (var participant in _participants)
            {
                try
                {
                    var vote = participant.Prepare();
                    votes.Add(vote);
                }
                catch (Exception)
                {
                    // Any exception is treated as an abort vote.
                    votes.Add(Vote.Abort);
                }
            }

            // Phase 2 – Decision
            _finalDecision = votes.All(v => v == Vote.Commit) ? Decision.Commit : Decision.Abort;
            _state = CoordinatorState.Decided;

            // Phase 3 – Broadcast decision
            foreach (var participant in _participants)
            {
                try
                {
                    participant.ReceiveDecision(_finalDecision.Value);
                }
                catch (Exception)
                {
                    // In a pure in‑memory simulation we ignore participant failures during broadcast.
                }
            }

            _state = CoordinatorState.Completed;
            return _finalDecision.Value;
        }
    }
}
