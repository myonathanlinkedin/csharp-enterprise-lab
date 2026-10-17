using System;
using System.Diagnostics;
using LamportSync;

namespace LamportSync
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        }

        private static void RunTests()
        {
            var engine = new LamportEngine();

            // Register two processes: A and B
            engine.RegisterProcess("A");
            engine.RegisterProcess("B");

            // Initial timestamps must be zero
            Assert(engine.GetCurrent("A") == new Timestamp(0), "A initial timestamp");
            Assert(engine.GetCurrent("B") == new Timestamp(0), "B initial timestamp");

            // Process A internal event -> T=1
            var tA1 = engine.InternalEvent("A");
            Assert(tA1 == new Timestamp(1), "A internal event 1");
            Assert(engine.GetCurrent("A") == tA1, "A current after internal");

            // Process A sends a message -> T=2 (timestamp attached)
            var tA2 = engine.SendEvent("A");
            Assert(tA2 == new Timestamp(2), "A send event");
            Assert(engine.GetCurrent("A") == tA2, "A current after send");

            // Process B receives the message with timestamp 2
            var tB1 = engine.ReceiveEvent("B", tA2);
            // Expected: max(0,2)+1 = 3
            Assert(tB1 == new Timestamp(3), "B receive from A");
            Assert(engine.GetCurrent("B") == tB1, "B current after receive");

            // Process B internal event -> T=4
            var tB2 = engine.InternalEvent("B");
            Assert(tB2 == new Timestamp(4), "B internal event");
            Assert(engine.GetCurrent("B") == tB2, "B current after internal");

            // Process B sends a message -> T=5
            var tB3 = engine.SendEvent("B");
            Assert(tB3 == new Timestamp(5), "B send event");
            Assert(engine.GetCurrent("B") == tB3, "B current after send");

            // Process A receives the message with timestamp 5
            var tA3 = engine.ReceiveEvent("A", tB3);
            // Expected: max(2,5)+1 = 6
            Assert(tA3 == new Timestamp(6), "A receive from B");
            Assert(engine.GetCurrent("A") == tA3, "A current after receive");

            // Verify monotonicity: timestamps never decrease
            Assert(tA3 > tA2, "A monotonic increase");
            Assert(tB3 > tB2, "B monotonic increase");

            // Edge case: receiving a lower timestamp should still increment
            // A sends again (T=7)
            var tA4 = engine.SendEvent("A");
            Assert(tA4 == new Timestamp(7), "A second send");
            // B receives timestamp 7, but B's clock is 5, so new = max(5,7)+1 = 8
            var tB4 = engine.ReceiveEvent("B", tA4);
            Assert(tB4 == new Timestamp(8), "B receive higher timestamp");
            // Now B sends a message with timestamp 9
            var tB5 = engine.SendEvent("B");
            Assert(tB5 == new Timestamp(9), "B second send");
            // A receives timestamp 9, its clock is 7, new = max(7,9)+1 = 10
            var tA5 = engine.ReceiveEvent("A", tB5);
            Assert(tA5 == new Timestamp(10), "A receive higher timestamp");

            // Verify exception handling for unknown process
            bool threw = false;
            try
            {
                engine.InternalEvent("C");
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "Exception on unknown process");

            // Verify exception on duplicate registration
            threw = false;
            try
            {
                engine.RegisterProcess("A");
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "Exception on duplicate registration");

            Console.WriteLine("All LamportEngine tests passed.");
        }

        static void Main()
        {
            RunTests();
        }
    }
}
