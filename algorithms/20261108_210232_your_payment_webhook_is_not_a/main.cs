using System;
using System.Collections.Generic;
using WebhookSystem;

namespace WebhookSystem
{
    // Minimal assertion helper – throws on failure.
    internal static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition) throw new InvalidOperationException("Assert.IsTrue failed. " + message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Assert.AreEqual failed. Expected:{expected} Actual:{actual}. {message}");
        }
    }

    internal static class Tests
    {
        // Captures processed payloads for verification.
        private static readonly List<string> Processed = new();

        private static void Handler(PaymentEvent ev) => Processed.Add($"{ev.PaymentId}:{ev.SequenceNumber}");

        public static void RunAll()
        {
            TestInOrder();
            TestOutOfOrder();
            TestDuplicates();
            TestMultiplePaymentsInterleaved();
            Console.WriteLine("All tests passed.");
        }

        private static void Reset()
        {
            Processed.Clear();
        }

        private static PaymentEvent NewEvent(string paymentId, int seq)
            => new PaymentEvent(Guid.NewGuid(), paymentId, seq, $"payload-{seq}");

        private static void TestInOrder()
        {
            Reset();
            var processor = new WebhookProcessor(Handler);
            var ev1 = NewEvent("payA", 1);
            var ev2 = NewEvent("payA", 2);
            var ev3 = NewEvent("payA", 3);

            Assert.IsTrue(processor.ProcessEvent(ev1));
            Assert.IsTrue(processor.ProcessEvent(ev2));
            Assert.IsTrue(processor.ProcessEvent(ev3));

            Assert.AreEqual(3, Processed.Count);
            Assert.AreEqual("payA:1", Processed[0]);
            Assert.AreEqual("payA:2", Processed[1]);
            Assert.AreEqual("payA:3", Processed[2]);
        }

        private static void TestOutOfOrder()
        {
            Reset();
            var processor = new WebhookProcessor(Handler);
            var ev2 = NewEvent("payB", 2);
            var ev1 = NewEvent("payB", 1);
            var ev3 = NewEvent("payB", 3);

            // Arrive out of order: 2 first (queued), then 1 (processes 1 and flushes 2), then 3.
            Assert.IsTrue(!processor.ProcessEvent(ev2));
            Assert.IsTrue(processor.ProcessEvent(ev1));
            Assert.IsTrue(processor.ProcessEvent(ev3));

            Assert.AreEqual(3, Processed.Count);
            Assert.AreEqual("payB:1", Processed[0]);
            Assert.AreEqual("payB:2", Processed[1]);
            Assert.AreEqual("payB:3", Processed[2]);
        }

        private static void TestDuplicates()
        {
            Reset();
            var processor = new WebhookProcessor(Handler);
            var ev1 = NewEvent("payC", 1);
            var dup = new PaymentEvent(ev1.Id, ev1.PaymentId, ev1.SequenceNumber, ev1.Payload);

            Assert.IsTrue(processor.ProcessEvent(ev1));
            Assert.IsTrue(!processor.ProcessEvent(dup)); // Duplicate Id.
            Assert.AreEqual(1, Processed.Count);
            Assert.AreEqual("payC:1", Processed[0]);
        }

        private static void TestMultiplePaymentsInterleaved()
        {
            Reset();
            var processor = new WebhookProcessor(Handler);
            var a1 = NewEvent("A", 1);
            var b1 = NewEvent("B", 1);
            var a2 = NewEvent("A", 2);
            var b2 = NewEvent("B", 2);
            var a3 = NewEvent("A", 3);
            var b3 = NewEvent("B", 3);

            // Interleave out‑of‑order for A, in‑order for B.
            Assert.IsTrue(processor.ProcessEvent(a2)); // queued
            Assert.IsTrue(processor.ProcessEvent(b1)); // processed
            Assert.IsTrue(processor.ProcessEvent(a1)); // processes A1 then flushes A2
            Assert.IsTrue(processor.ProcessEvent(b2)); // processed
            Assert.IsTrue(processor.ProcessEvent(a3)); // processed
            Assert.IsTrue(processor.ProcessEvent(b3)); // processed

            var expected = new[]
            {
                "A:1","A:2","A:3",
                "B:1","B:2","B:3"
            };
            Assert.AreEqual(expected.Length, Processed.Count);
            // Order within each payment is guaranteed; overall order may intermix.
            // Verify each expected entry appears.
            foreach (var exp in expected)
                Assert.IsTrue(Processed.Contains(exp), $"Missing {exp}");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Tests.RunAll();
        }
    }
}
