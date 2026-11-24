using System;
using System.Collections.Generic;

namespace TwoPhaseCommit
{
    // Possible outcomes of a participant's vote
    public enum Vote
    {
        Commit,
        Abort
    }

    // Decision sent by the coordinator after collecting votes
    public enum Decision
    {
        Commit,
        Abort
    }

    // States of a participant in the 2PC protocol
    public enum ParticipantState
    {
        Init,           // Not yet involved in a transaction
        Prepared,       // Voted Commit, waiting for final decision
        Committed,      // Transaction committed
        Aborted         // Transaction aborted
    }

    // States of the coordinator in the 2PC protocol
    public enum CoordinatorState
    {
        Init,           // No transaction started
        Collecting,     // Sending prepare requests, gathering votes
        Decided,        // Decision made (Commit or Abort)
        Completed       // Decision communicated to all participants
    }

    // Interface that a participant must implement
    public interface IParticipant
    {
        // Unique identifier for debugging / tracing
        Guid Id { get; }

        // Current state of the participant
        ParticipantState State { get; }

        // Called by the coordinator to ask the participant to prepare.
        // Returns the participant's vote.
        Vote Prepare();

        // Called by the coordinator to inform the final decision.
        void ReceiveDecision(Decision decision);
    }

    // Simple data structure representing a transaction identifier
    public readonly struct TransactionId : IEquatable<TransactionId>
    {
        public Guid Id { get; }

        public TransactionId(Guid id) => Id = id;

        public static TransactionId NewId() => new TransactionId(Guid.NewGuid());

        public bool Equals(TransactionId other) => Id.Equals(other.Id);
        public override bool Equals(object obj) => obj is TransactionId other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id.ToString();
    }
}
