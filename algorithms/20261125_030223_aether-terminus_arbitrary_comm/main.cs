using System;
using System.Collections.Generic;
using AetherTerminus;

namespace AetherTerminus.Tests
{
    public static class Program
    {
        private static int _passed;
        private static int _failed;

        private static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  PASS: {testName}");
            }
            else
            {
                _failed++;
                Console.WriteLine($"  FAIL: {testName}");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            bool eq = EqualityComparer<T>.Default.Equals(expected, actual);
            if (eq)
            {
                _passed++;
                Console.WriteLine($"  PASS: {testName}");
            }
            else
            {
                _failed++;
                Console.WriteLine($"  FAIL: {testName} (expected={expected}, actual={actual})");
            }
        }

        public static int Main()
        {
            Console.WriteLine("=== Aether-Terminus APC Injection Detector: Unit Tests ===\n");

            TestFrameSerialization();
            TestFrameValidation();
            TestNoInjection();
            TestSingleInjection();
            TestMultipleInjections();
            TestAlertableFlag();
            TestPayloadBoundary();
            TestTruncatedFrame();
            TestInvalidMagic();
            TestQueueDispatch();
            TestQueueAlertableGating();
            TestQueueDrain();
            TestEmptyPayload();
            TestMaxPayloadClamp();
            TestNullInputs();

            Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed, {_passed + _failed} total ===");
            return _failed == 0 ? 0 : 1;
        }

        private static void TestFrameSerialization()
        {
            Console.WriteLine("[Frame Serialization]");
            byte[] payload = { 0x01, 0x02, 0x03 };
            var frame = new ChronosFrame(ChronosFrame.Magic, (ushort)3, payload);
            byte[] wire = frame.ToBytes();
            AssertEqual(9, wire.Length, "Wire length = 6 + 3");
            var roundTrip = ChronosFrame.FromBytes(wire);
            AssertEqual(ChronosFrame.Magic, roundTrip.MagicValue, "Magic preserved");
            AssertEqual((ushort)3, roundTrip.PayloadLength, "Length preserved");
            Assert(roundTrip.Payload[0] == 0x01 && roundTrip.Payload[1] == 0x02 && roundTrip.Payload[2] == 0x03, "Payload preserved");
        }

        private static void TestFrameValidation()
        {
            Console.WriteLine("[Frame Validation]");
            var good = new ChronosFrame(ChronosFrame.Magic, (ushort)2, new byte[] { 0xAA, 0xBB });
            Assert(good.IsValid, "Valid frame passes");
            var badMagic = new ChronosFrame(0xDEADBEEF, (ushort)2, new byte[] { 0xAA, 0xBB });
            Assert(!badMagic.IsValid, "Bad magic fails");
            var badLen = new ChronosFrame(ChronosFrame.Magic, (ushort)5, new byte[] { 0xAA, 0xBB });
            Assert(!badLen.IsValid, "Length mismatch fails");
        }

        private static void TestNoInjection()
        {
            Console.WriteLine("[No Injection Detected]");
            var detector = new ApcInjectionDetector();
            byte[] benign = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };
            var results = detector.Scan(benign);
            AssertEqual(0, results.Count, "No APCs in benign payload");
        }

        private static void TestSingleInjection()
        {
            Console.WriteLine("[Single Injection]");
            var detector = new ApcInjectionDetector();
            IntPtr routine = new IntPtr(0x7FF000001234);
            IntPtr ctx1 = new IntPtr(0x00000000DEAD);
            IntPtr ctx2 = new IntPtr(0x00000000BEEF);
            byte[] apcData = { 0xCA, 0xFE, 0xBA, 0xBE };
            var frame = ApcInjectionDetector.BuildInjectionFrame(routine, ctx1, ctx2, false, apcData);
            var results = detector.AnalyzeFrame(frame);
            AssertEqual(1, results.Count, "One APC detected");
            AssertEqual(routine, results[0].ApcRoutine, "Routine match");
            AssertEqual(ctx1, results[0].ApcContext1, "Ctx1 match");
            AssertEqual(ctx2, results[0].ApcContext2, "Ctx2 match");
            Assert(!results[0].IsAlertable, "Non-alertable");
            AssertEqual(4, results[0].Payload.Length, "APC payload length");
        }

        private static void TestMultipleInjections()
        {
            Console.WriteLine("[Multiple Injections]");
            var detector = new ApcInjectionDetector();
            // Build two injections concatenated
            var f1 = ApcInjectionDetector.BuildInjectionFrame(new IntPtr(0x1000), new IntPtr(0x2000), new IntPtr(0x3000), false, new byte[] { 0x01 });
            var f2 = ApcInjectionDetector.BuildInjectionFrame(new IntPtr(0x4000), new IntPtr(0x5000), new IntPtr(0x6000), true, new byte[] { 0x02, 0x03 });
            byte[] combined = new byte[f1.Payload.Length + f2.Payload.Length];
            Array.Copy(f1.Payload, 0, combined, 0, f1.Payload.Length);
            Array.Copy(f2.Payload, 0, combined, f1.Payload.Length, f2.Payload.Length);
            var frame = new ChronosFrame(ChronosFrame.Magic, (ushort)combined.Length, combined);
            var results = detector.AnalyzeFrame(frame);
            AssertEqual(2, results.Count, "Two APCs detected");
            AssertEqual(new IntPtr(0x1000), results[0].ApcRoutine, "First routine");
            AssertEqual(new IntPtr(0x4000), results[1].ApcRoutine, "Second routine");
            Assert(results[1].IsAlertable, "Second is alertable");
        }

        private static void TestAlertableFlag()
        {
            Console.WriteLine("[Alertable Flag]");
            var detector = new ApcInjectionDetector();
            var frame = ApcInjectionDetector.BuildInjectionFrame(new IntPtr(0x1), new IntPtr(0x2), new IntPtr(0x3), true, new byte[] { 0x00 });
            var results = detector.AnalyzeFrame(frame);
            Assert(results[0].IsAlertable, "Alertable flag set");
        }

        private static void TestPayloadBoundary()
        {
            Console.WriteLine("[Payload Boundary]");
            var detector = new ApcInjectionDetector();
            // Empty APC payload
            var frame = ApcInjectionDetector.BuildInjectionFrame(new IntPtr(0x1), new IntPtr(0x2), new IntPtr(0x3), false, Array.Empty<byte>());
            var results = detector.AnalyzeFrame(frame);
            AssertEqual(1, results.Count, "One APC with empty payload");
            AssertEqual(0, results[0].Payload.Length, "Empty payload length");
        }

        private static void TestTruncatedFrame()
        {
            Console.WriteLine("[Truncated Frame]");
            var detector = new ApcInjectionDetector();
            byte[] truncated = { 0x41, 0x50, 0x43, 0x5F }; // partial signature
            var results = detector.Scan(truncated);
            AssertEqual(0, results.Count, "Truncated signature yields no APCs");
        }

        private static void TestInvalidMagic()
        {
            Console.WriteLine("[Invalid Magic]");
            var detector = new ApcInjectionDetector();
            var badFrame = new ChronosFrame(0x12345678, (ushort)4, new byte[] { 0x01, 0x02, 0x03, 0x04 });
            bool threw = false;
            try { detector.AnalyzeFrame(badFrame); }
            catch (InvalidOperationException) { threw = true; }
            Assert(threw, "Invalid magic throws InvalidOperationException");
        }

        private static void TestQueueDispatch()
        {
            Console.WriteLine("[Queue Dispatch]");
            var sim = new ApcQueueSimulator();
            var r1 = new ApcRecord(new IntPtr(0x1), new IntPtr(0), new IntPtr(0), false, Array.Empty<byte>());
            var r2 = new ApcRecord(new IntPtr(0x2), new IntPtr(0), new IntPtr(0), false, Array.Empty<byte>());
            sim.Enqueue(r1);
            sim.Enqueue(r2);
            AssertEqual(2, sim.Count, "Queue count = 2");
            var d1 = sim.DequeueNext();
            Assert(d1 != null && d1.ApcRoutine == new IntPtr(0x1), "FIFO: first out");
            var d2 = sim.DequeueNext();
            Assert(d2 != null && d2.ApcRoutine == new IntPtr(0x2), "FIFO: second out");
            var d3 = sim.DequeueNext();
            Assert(d3 == null, "Queue empty returns null");
        }

        private static void TestQueueAlertableGating()
        {
            Console.WriteLine("[Alertable Gating]");
            var sim = new ApcQueueSimulator();
            var alertable = new ApcRecord(new IntPtr(0xA), new IntPtr(0), new IntPtr(0), true, Array.Empty<byte>());
            sim.Enqueue(alertable);
            // Not in alertable wait: cannot dispatch
            var d = sim.DequeueNext();
            Assert(d == null, "Alertable APC blocked outside alertable wait");
            AssertEqual(1, sim.Count, "APC still in queue");
            // Enter alertable wait: can dispatch
            sim.EnterAlertableWait();
            var d2 = sim.DequeueNext();
            Assert(d2 != null && d2.ApcRoutine == new IntPtr(0xA), "Alertable APC dispatched in wait");
            AssertEqual(0, sim.Count, "Queue empty after dispatch");
        }

        private static void TestQueueDrain()
        {
            Console.WriteLine("[Queue Drain]");
            var sim = new ApcQueueSimulator();
            for (int i = 0; i < 5; i++)
                sim.Enqueue(new ApcRecord(new IntPtr(i), new IntPtr(0), new IntPtr(0), false, Array.Empty<byte>()));
            int drained = sim.DrainAll();
            AssertEqual(5, drained, "Drained 5 APCs");
            AssertEqual(0, sim.Count, "Queue empty after drain");
        }

        private static void TestEmptyPayload()
        {
            Console.WriteLine("[Empty Payload]");
            var detector = new ApcInjectionDetector();
            var results = detector.Scan(Array.Empty<byte>());
            AssertEqual(0, results.Count, "Empty payload yields no APCs");
        }

        private static void TestMaxPayloadClamp()
        {
            Console.WriteLine("[Max Payload Clamp]");
            var detector = new ApcInjectionDetector();
            // Build a frame with payload length byte set to max (0x7F = 127)
            // but actual data is shorter; detector should clamp.
            byte[] sig = { 0x41, 0x50, 0x43, 0x5F, 0x49, 0x4E, 0x4A, 0x45, 0x43, 0x54, 0x5F, 0x56, 0x31, 0x00, 0x00, 0x00 };
            byte[] header = new byte[24]; // zeros
            byte[] flagByte = { 0x7E }; // 0x7E = (127 << 1) | 0 => payloadLen=127, alertable=false
            byte[] shortData = { 0x01, 0x02 }; // only 2 bytes available
            byte[] payload = new byte[16 + 24 + 1 + 2];
            Array.Copy(sig, 0, payload, 0, 16);
            Array.Copy(header, 0, payload, 16, 24);
            payload[40] = 0x7E;
            Array.Copy(shortData, 0, payload, 41, 2);
            var results = detector.Scan(payload);
            AssertEqual(1, results.Count, "One APC detected with clamped payload");
            AssertEqual(2, results[0].Payload.Length, "Payload clamped to available bytes");
        }

        private static void TestNullInputs()
        {
            Console.WriteLine("[Null Inputs]");
            var detector = new ApcInjectionDetector();
            bool threw1 = false;
            try { detector.AnalyzeFrame(null); }
            catch (ArgumentNullException) { threw1 = true; }
            Assert(threw1, "Null frame throws ArgumentNullException");

            var sim = new ApcQueueSimulator();
            bool threw2 = false;
            try { sim.Enqueue(null); }
            catch (ArgumentNullException) { threw2 = true; }
            Assert(threw2, "Null record throws ArgumentNullException");
        }
    }
}
