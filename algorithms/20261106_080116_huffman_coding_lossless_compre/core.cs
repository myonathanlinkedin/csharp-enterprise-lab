using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuffmanCompression
{
    // Huffman tree node
    internal sealed class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte? Symbol { get; }
        public int Frequency { get; }
        public HuffmanNode? Left { get; }
        public HuffmanNode? Right { get; }

        public HuffmanNode(byte symbol, int freq)
        {
            Symbol = symbol;
            Frequency = freq;
        }

        public HuffmanNode(HuffmanNode left, HuffmanNode right)
        {
            Symbol = null;
            Frequency = left.Frequency + right.Frequency;
            Left = left;
            Right = right;
        }

        public int CompareTo(HuffmanNode? other)
        {
            if (other == null) return 1;
            int cmp = Frequency.CompareTo(other.Frequency);
            if (cmp != 0) return cmp;
            // Ensure deterministic ordering when frequencies equal
            return (Symbol ?? 0).CompareTo(other.Symbol ?? 0);
        }

        public bool IsLeaf => Symbol.HasValue;
    }

    // Helper to write bits into a byte list
    internal sealed class BitWriter
    {
        private readonly List<byte> _bytes = new();
        private int _current = 0;
        private int _filled = 0; // number of bits filled in _current (0..7)

        public void WriteBits(string bits)
        {
            foreach (char c in bits)
            {
                _current = (_current << 1) | (c == '1' ? 1 : 0);
                _filled++;
                if (_filled == 8)
                {
                    _bytes.Add((byte)_current);
                    _current = 0;
                    _filled = 0;
                }
            }
        }

        public byte[] ToArray()
        {
            if (_filled > 0)
            {
                _current <<= (8 - _filled);
                _bytes.Add((byte)_current);
            }
            return _bytes.ToArray();
        }
    }

    // Helper to read bits from a byte array
    internal sealed class BitReader
    {
        private readonly byte[] _data;
        private int _bytePos = 0;
        private int _bitPos = 0; // 0..7 (next bit to read)

        public BitReader(byte[] data) => _data = data;

        // Returns -1 when no more bits
        public int ReadBit()
        {
            if (_bytePos >= _data.Length) return -1;
            int bit = (_data[_bytePos] >> (7 - _bitPos)) & 1;
            _bitPos++;
            if (_bitPos == 8)
            {
                _bitPos = 0;
                _bytePos++;
            }
            return bit;
        }
    }

    public static class HuffmanCodec
    {
        // Build frequency table
        private static int[] BuildFrequencyTable(byte[] data)
        {
            var freq = new int[256];
            foreach (byte b in data) freq[b]++;
            return freq;
        }

        // Build Huffman tree from frequency table
        private static HuffmanNode BuildTree(int[] freq)
        {
            var pq = new PriorityQueue<HuffmanNode, int>();
            for (int i = 0; i < freq.Length; i++)
                if (freq[i] > 0)
                    pq.Enqueue(new HuffmanNode((byte)i, freq[i]), freq[i]);

            // Edge case: only one distinct symbol
            if (pq.Count == 1)
            {
                var only = pq.Dequeue();
                // Create a dummy parent to ensure a two‑node tree
                var parent = new HuffmanNode(only, new HuffmanNode(0, 0));
                return parent;
            }

            while (pq.Count > 1)
            {
                var left = pq.Dequeue();
                var right = pq.Dequeue();
                var parent = new HuffmanNode(left, right);
                pq.Enqueue(parent, parent.Frequency);
            }

            return pq.Dequeue();
        }

        // Generate code table (symbol -> bit string)
        private static void GenerateCodes(HuffmanNode node, string prefix, Dictionary<byte, string> table)
        {
            if (node.IsLeaf)
            {
                table[node.Symbol!.Value] = prefix.Length == 0 ? "0" : prefix; // handle single‑symbol case
                return;
            }
            if (node.Left != null) GenerateCodes(node.Left, prefix + "0", table);
            if (node.Right != null) GenerateCodes(node.Right, prefix + "1", table);
        }

        // Serialize header: count (ushort) + for each symbol: byte + int frequency
        private static byte[] SerializeHeader(int[] freq)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            ushort distinct = (ushort)freq.Count(f => f > 0);
            bw.Write(distinct);
            for (int i = 0; i < freq.Length; i++)
                if (freq[i] > 0)
                {
                    bw.Write((byte)i);
                    bw.Write(freq[i]);
                }
            return ms.ToArray();
        }

        // Deserialize header back to frequency table
        private static int[] DeserializeHeader(BinaryReader br)
        {
            var freq = new int[256];
            ushort distinct = br.ReadUInt16();
            for (int i = 0; i < distinct; i++)
            {
                byte sym = br.ReadByte();
                int f = br.ReadInt32();
                freq[sym] = f;
            }
            return freq;
        }

        // Public API: compress raw bytes to Huffman format
        public static byte[] Compress(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return Array.Empty<byte>();

            int[] freq = BuildFrequencyTable(data);
            HuffmanNode root = BuildTree(freq);
            var codeTable = new Dictionary<byte, string>();
            GenerateCodes(root, "", codeTable);

            var writer = new BitWriter();
            foreach (byte b in data)
                writer.WriteBits(codeTable[b]);

            byte[] body = writer.ToArray();
            byte[] header = SerializeHeader(freq);

            // Concatenate header + body
            var result = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(body, 0, result, header.Length, body.Length);
            return result;
        }

        // Public API: decompress Huffman format back to original bytes
        public static byte[] Decompress(byte[] compressed)
        {
            if (compressed == null) throw new ArgumentNullException(nameof(compressed));
            if (compressed.Length == 0) return Array.Empty<byte>();

            using var ms = new MemoryStream(compressed);
            using var br = new BinaryReader(ms);
            int[] freq = DeserializeHeader(br);
            HuffmanNode root = BuildTree(freq);

            // Remaining bytes are the bitstream
            byte[] body = br.ReadBytes((int)(ms.Length - ms.Position));
            var reader = new BitReader(body);
            var output = new List<byte>();
            HuffmanNode node = root;

            while (true)
            {
                int bit = reader.ReadBit();
                if (bit == -1) break; // no more bits

                node = bit == 0 ? node.Left! : node.Right!;
                if (node.IsLeaf)
                {
                    output.Add(node.Symbol!.Value);
                    node = root;
                }
            }

            // Validate that we consumed exactly the expected number of symbols
            int expected = freq.Sum();
            if (output.Count != expected)
                throw new InvalidOperationException("Decompression failed: symbol count mismatch.");

            return output.ToArray();
        }
    }
}
