using System;
using System.Collections.Generic;
using System.Text;

namespace AetherTerminus
{
    /// <summary>
    /// Models a single APC (Asynchronous Procedure Call) record as it would appear
    /// in a Windows kernel APC queue. This is a pure data model for simulation.
    /// </summary>
    public sealed class ApcRecord
    {
        public IntPtr ApcRoutine { get; }
        public IntPtr ApcContext1 { get; }
        public IntPtr ApcContext2 { get; }
        public bool IsAlertable { get; }
        public byte[] Payload { get; }

        public ApcRecord(IntPtr routine, IntPtr ctx1, IntPtr ctx2, bool alertable, byte[] payload)
        {
            ApcRoutine = routine;
            ApcContext1 = ctx1;
            ApcContext2 = ctx2;
            IsAlertable = alertable;
            Payload = payload ?? Array.Empty<byte>();
        }

        public override string ToString() =>
            $"Apc(routine=0x{ApcRoutine:X}, ctx1=0x{ApcContext1:X}, ctx2=0x{ApcContext2:X}, alertable={IsAlertable}, payload={Payload.Length}B)";
    }

    /// <summary>
    /// Simulates a Chronos-Viewport image protocol frame.
    /// A frame consists of a 4-byte magic, 2-byte length, and payload.
    /// The payload may contain embedded APC injection directives.
    /// </summary>
    public sealed class ChronosFrame
    {
        public const uint Magic = 0x43524E53; // "CRNS"
        public uint MagicValue { get; }
        public ushort PayloadLength { get; }
        public byte[] Payload { get; }

        public ChronosFrame(uint magic, ushort length, byte[] payload)
        {
            MagicValue = magic;
            PayloadLength = length;
            Payload = payload ?? Array.Empty<byte>();
        }

        public bool IsValid => MagicValue == Magic && PayloadLength == Payload.Length;

        public static ChronosFrame FromBytes(byte[] data)
        {
            if (data == null || data.Length < 6)
                throw new ArgumentException("Frame too short", nameof(data));
            uint magic = BitConverter.ToUInt32(data, 0);
            ushort len = BitConverter.ToUInt16(data, 4);
            if (data.Length < 6 + len)
                throw new ArgumentException("Truncated frame", nameof(data));
            byte[] payload = new byte[len];
            Array.Copy(data, 6, payload, 0, len);
            return new ChronosFrame(magic, len, payload);
        }

        public byte[] ToBytes()
        {
            byte[] buf = new byte[6 + PayloadLength];
            Buffer.BlockCopy(BitConverter.GetBytes(MagicValue), 0, buf, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(PayloadLength), 0, buf, 4, 2);
            Array.Copy(Payload, 0, buf, 6, PayloadLength);
            return buf;
        }
    }

    /// <summary>
    /// APC Response Injection Detector.
    /// Scans Chronos-Viewport frames for embedded APC injection directives.
    /// An APC directive is identified by a 16-byte signature followed by
    /// routine/context pointers and a payload length.
    ///
    /// Time complexity: O(n) where n = frame payload length.
    /// Space complexity: O(k) where k = number of detected APC records.
    /// </summary>
    public sealed class ApcInjectionDetector
    {
        // APC directive signature: "APC_INJECT_V1\0" (16 bytes)
        private static readonly byte[] ApcSignature =
            new byte[] { 0x41, 0x50, 0x43, 0x5F, 0x49, 0x4E, 0x4A, 0x45, 0x43, 0x54, 0x5F, 0x56, 0x31, 0x00, 0x00, 0x00 };

        private const int SignatureLength = 16;
        private const int HeaderSize = 24; // 8 (routine) + 8 (ctx1) + 8 (ctx2)
        private const int MinDirectiveSize = SignatureLength + HeaderSize + 1; // +1 for payload length byte
        private const int MaxPayloadSize = 4096;

        /// <summary>
        /// Scans a frame payload for APC injection directives.
        /// Returns a list of detected ApcRecord instances.
        /// </summary>
        public List<ApcRecord> Scan(byte[] payload)
        {
            var results = new List<ApcRecord>();
            if (payload == null || payload.Length < MinDirectiveSize)
                return results;

            int i = 0;
            while (i <= payload.Length - SignatureLength)
            {
                if (MatchesSignature(payload, i))
                {
                    int offset = i + SignatureLength;
                    if (offset + HeaderSize + 1 > payload.Length)
                        break;

                    IntPtr routine = new IntPtr(BitConverter.ToInt64(payload, offset));
                    IntPtr ctx1 = new IntPtr(BitConverter.ToInt64(payload, offset + 8));
                    IntPtr ctx2 = new IntPtr(BitConverter.ToInt64(payload, offset + 16));
                    bool alertable = (payload[offset + 24] & 0x01) != 0;
                    int payloadLen = payload[offset + 24] >> 1; // upper 7 bits

                    if (payloadLen > MaxPayloadSize)
                        payloadLen = MaxPayloadSize;

                    int dataStart = offset + 25;
                    if (dataStart + payloadLen > payload.Length)
                        payloadLen = payload.Length - dataStart;

                    byte[] apcPayload = new byte[payloadLen];
                    Array.Copy(payload, dataStart, apcPayload, 0, payloadLen);

                    results.Add(new ApcRecord(routine, ctx1, ctx2, alertable, apcPayload));
                    i = dataStart + payloadLen; // skip past this directive
                }
                else
                {
                    i++;
                }
            }
            return results;
        }

        /// <summary>
        /// Validates a complete ChronosFrame and extracts any APC injections.
        /// </summary>
        public List<ApcRecord> AnalyzeFrame(ChronosFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (!frame.IsValid)
                throw new InvalidOperationException("Invalid Chronos frame: bad magic or length mismatch.");
            return Scan(frame.Payload);
        }

        /// <summary>
        /// Constructs a malicious frame containing an APC injection directive.
        /// Used for red-team simulation and test generation.
        /// </summary>
        public static ChronosFrame BuildInjectionFrame(IntPtr routine, IntPtr ctx1, IntPtr ctx2, bool alertable, byte[] apcPayload)
        {
            int payloadLen = apcPayload?.Length ?? 0;
            if (payloadLen > MaxPayloadSize)
                throw new ArgumentException("APC payload exceeds maximum size", nameof(apcPayload));

            int totalLen = SignatureLength + HeaderSize + 1 + payloadLen;
            byte[] buf = new byte[totalLen];
            int pos = 0;

            Array.Copy(ApcSignature, 0, buf, pos, SignatureLength);
            pos += SignatureLength;

            Buffer.BlockCopy(BitConverter.GetBytes(routine.ToInt64()), 0, buf, pos, 8);
            pos += 8;
            Buffer.BlockCopy(BitConverter.GetBytes(ctx1.ToInt64()), 0, buf, pos, 8);
            pos += 8;
            Buffer.BlockCopy(BitConverter.GetBytes(ctx2.ToInt64()), 0, buf, pos, 8);
            pos += 8;

            byte flags = (byte)((alertable ? 0x01 : 0x00) | ((payloadLen & 0x7F) << 1));
            buf[pos] = flags;
            pos += 1;

            if (payloadLen > 0)
                Array.Copy(apcPayload, 0, buf, pos, payloadLen);

            return new ChronosFrame(ChronosFrame.Magic, (ushort)totalLen, buf);
        }

        private static bool MatchesSignature(byte[] data, int offset)
        {
            for (int j = 0; j < SignatureLength; j++)
            {
                if (data[offset + j] != ApcSignature[j])
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// APC Queue Simulator. Maintains an ordered queue of APC records
    /// and supports alertable/non-alertable dispatch semantics.
    ///
    /// Invariant: Queue is FIFO. Alertable APCs are dispatched only when
    /// the simulated thread enters an alertable wait state.
    /// </summary>
    public sealed class ApcQueueSimulator
    {
        private readonly Queue<ApcRecord> _queue = new Queue<ApcRecord>();
        private bool _inAlertableWait;

        public int Count => _queue.Count;
        public bool InAlertableWait => _inAlertableWait;

        public void Enqueue(ApcRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            _queue.Enqueue(record);
        }

        public void EnterAlertableWait() => _inAlertableWait = true;
        public void ExitAlertableWait() => _inAlertableWait = false;

        /// <summary>
        /// Attempts to dequeue and "execute" the next APC.
        /// Non-alertable APCs are always dispatched.
        /// Alertable APCs are dispatched only if in alertable wait.
        /// Returns the dispatched record, or null if none available.
        /// </summary>
        public ApcRecord? DequeueNext()
        {
            while (_queue.Count > 0)
            {
                var front = _queue.Peek();
                if (!front.IsAlertable || _inAlertableWait)
                {
                    return _queue.Dequeue();
                }
                // Alertable APC but not in alertable wait: cannot dispatch.
                // In a real kernel, the thread would block. Here we return null
                // to indicate no dispatchable APC is available.
                return null;
            }
            return null;
        }

        /// <summary>
        /// Drains all dispatchable APCs. Returns count dispatched.
        /// </summary>
        public int DrainAll()
        {
            int count = 0;
            while (true)
            {
                var rec = DequeueNext();
                if (rec == null) break;
                count++;
            }
            return count;
        }
    }
}
