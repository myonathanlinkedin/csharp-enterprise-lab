using System;
using System.Collections.Generic;

namespace WebhookSystem
{
    // Represents a webhook event for a payment.
    public sealed class PaymentEvent : IComparable<PaymentEvent>
    {
        public Guid Id { get; }
        public string PaymentId { get; }
        public int SequenceNumber { get; }
        public string Payload { get; }

        public PaymentEvent(Guid id, string paymentId, int sequenceNumber, string payload)
        {
            Id = id;
            PaymentId = paymentId ?? throw new ArgumentNullException(nameof(paymentId));
            SequenceNumber = sequenceNumber;
            Payload = payload ?? string.Empty;
        }

        // Sorted by SequenceNumber, then Id to guarantee deterministic ordering.
        public int CompareTo(PaymentEvent? other)
        {
            if (other is null) return 1;
            int cmp = SequenceNumber.CompareTo(other.SequenceNumber);
            if (cmp != 0) return cmp;
            return Id.CompareTo(other.Id);
        }

        public override bool Equals(object? obj) =>
            obj is PaymentEvent other && Id.Equals(other.Id);

        public override int GetHashCode() => Id.GetHashCode();
    }

    // Core processor guaranteeing exactly‑once, in‑order handling per payment.
    public sealed class WebhookProcessor
    {
        private readonly Action<PaymentEvent> _handler;

        // Last successfully processed sequence per payment.
        private readonly Dictionary<string, int> _lastSequence = new();

        // Pending events that arrived out of order, kept sorted.
        private readonly Dictionary<string, SortedSet<PaymentEvent>> _pending = new();

        // Tracks processed event IDs to guarantee idempotence across payments.
        private readonly HashSet<Guid> _processedIds = new();

        public WebhookProcessor(Action<PaymentEvent> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        // Returns true if the event caused a user‑visible processing step.
        public bool ProcessEvent(PaymentEvent ev)
        {
            if (ev is null) throw new ArgumentNullException(nameof(ev));

            // Global deduplication by unique Id.
            if (_processedIds.Contains(ev.Id))
                return false; // Duplicate delivery.

            // Ensure dictionaries have entries for this payment.
            if (!_lastSequence.TryGetValue(ev.PaymentId, out int lastSeq))
                lastSeq = 0; // No events processed yet.

            // If the event is the immediate next in order, process it.
            if (ev.SequenceNumber == lastSeq + 1)
            {
                ApplyEvent(ev);
                // After processing, flush any pending consecutive events.
                FlushPending(ev.PaymentId);
                return true;
            }

            // If the event is older or equal to last processed, treat as duplicate.
            if (ev.SequenceNumber <= lastSeq)
                return false;

            // Event is ahead of expected sequence: store for later.
            if (!_pending.TryGetValue(ev.PaymentId, out var set))
            {
                set = new SortedSet<PaymentEvent>();
                _pending[ev.PaymentId] = set;
            }
            set.Add(ev);
            // Do not mark as processed yet.
            return false;
        }

        private void ApplyEvent(PaymentEvent ev)
        {
            // Record global Id to prevent future duplicates.
            _processedIds.Add(ev.Id);
            // Update last sequence for the payment.
            _lastSequence[ev.PaymentId] = ev.SequenceNumber;
            // Invoke user‑provided handler.
            _handler(ev);
        }

        private void FlushPending(string paymentId)
        {
            if (!_pending.TryGetValue(paymentId, out var set) || set.Count == 0)
                return;

            int expected = _lastSequence[paymentId] + 1;
            while (set.Count > 0)
            {
                var first = set.Min!;
                if (first.SequenceNumber != expected)
                    break; // Gap still exists.

                set.Remove(first);
                ApplyEvent(first);
                expected++;
            }

            if (set.Count == 0)
                _pending.Remove(paymentId);
        }
    }
}
