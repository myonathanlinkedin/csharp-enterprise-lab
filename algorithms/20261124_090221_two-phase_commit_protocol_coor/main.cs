using System;
using System.Collections.Generic;
using TwoPhaseCommit;

namespace TwoPhaseCommit
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void TestAllCommit()
        {
            var participants = new List<IParticipant>
            {
                new Participant(Vote.Commit),
                new Participant(Vote.Commit),
                new Participant(Vote.Commit)
            };
            var coordinator = new Coordinator(participants);
            var decision = coordinator.Execute();

            Assert(decision == Decision.Commit, "All participants should lead to Commit decision.");
            foreach (var p in participants)
                Assert(p.State == ParticipantState.Committed, "Participant should be in Committed state.");
            Assert(coordinator.State == CoordinatorState.Completed, "Coordinator should be Completed.");
        }

        private static void TestOneAbort()
        {
            var participants = new List<IParticipant>
            {
                new Participant(Vote.Commit),
                new Participant(Vote.Abort), // This participant forces abort
                new Participant(Vote.Commit)
            };
            var coordinator = new Coordinator(participants);
            var decision = coordinator.Execute();

            Assert(decision == Decision.Abort, "Presence of an Abort vote must lead to Abort decision.");
            foreach (var p in participants)
                Assert(p.State == ParticipantState.Aborted, "All participants must be Aborted.");
            Assert(coordinator.State == CoordinatorState.Completed, "Coordinator should be Completed.");
        }

        private static void TestExceptionDuringPrepare()
        {
            // Participant that throws during Prepare
            var faulty = new FaultyParticipant();
            var participants = new List<IParticipant>
            {
                new Participant(Vote.Commit),
                faulty,
                new Participant(Vote.Commit)
            };
            var coordinator = new Coordinator(participants);
            var decision = coordinator.Execute();

            Assert(decision == Decision.Abort, "Exception during prepare must be treated as Abort.");
            Assert(faulty.State == ParticipantState.Aborted, "Faulty participant must be Aborted.");
            foreach (var p in participants)
                Assert(p.State == ParticipantState.Aborted, "All participants must be Aborted after abort decision.");
        }

        // Helper participant that throws on Prepare()
        private sealed class FaultyParticipant : IParticipant
        {
            public Guid Id { get; } = Guid.NewGuid();
            public ParticipantState State { get; private set; } = ParticipantState.Init;

            public Vote Prepare()
            {
                State = ParticipantState.Aborted;
                throw new InvalidOperationException("Simulated failure during Prepare.");
            }

            public void ReceiveDecision(Decision decision)
            {
                // Follow same idempotent logic as normal participant
                if (decision == Decision.Abort && State != ParticipantState.Committed)
                    State = ParticipantState.Aborted;
                else if (decision == Decision.Commit && State == ParticipantState.Prepared)
                    State = ParticipantState.Committed;
            }
        }

        private static void TestIdempotentDecisionDelivery()
        {
            var participant = new Participant(Vote.Commit);
            var participants = new List<IParticipant> { participant };
            var coordinator = new Coordinator(participants);
            var decision = coordinator.Execute();

            Assert(decision == Decision.Commit, "Initial decision should be Commit.");
            // Simulate a duplicate decision message (e.g., network retry)
            participant.ReceiveDecision(Decision.Commit);
            Assert(participant.State == ParticipantState.Committed, "State must remain Committed after duplicate decision.");
        }

        private static void RunAllTests()
        {
            TestAllCommit();
            TestOneAbort();
            TestExceptionDuringPrepare();
            TestIdempotentDecisionDelivery();
        }

        public static void Main()
        {
            try
            {
                RunAllTests();
                Console.WriteLine("All Two‑Phase Commit tests passed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failure: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
