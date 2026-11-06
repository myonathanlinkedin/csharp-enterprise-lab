using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HuffmanCompression;

class Program
{
    static void Assert(bool condition, string message = "Assertion failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void TestRoundTrip(byte[] data)
    {
        byte[] compressed = HuffmanCodec.Compress(data);
        byte[] decompressed = HuffmanCodec.Decompress(compressed);
        Assert(data.SequenceEqual(decompressed), "Round‑trip data mismatch");
    }

    static void RunAllTests()
    {
        // Empty input
        TestRoundTrip(Array.Empty<byte>());

        // Single byte repeated
        TestRoundTrip(Enumerable.Repeat((byte)0x5A, 1000).ToArray());

        // Two distinct symbols
        var twoSym = new List<byte>();
        for (int i = 0; i < 500; i++) twoSym.Add(0x00);
        for (int i = 0; i < 300; i++) twoSym.Add(0xFF);
        TestRoundTrip(twoSym.ToArray());

        // All possible byte values with uniform frequency
        var allBytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        TestRoundTrip(allBytes);

        // Random data
        var rnd = new Random(42);
        var randomData = new byte[1024];
        rnd.NextBytes(randomData);
        TestRoundTrip(randomData);

        // Large data set (10 KB)
        var large = new byte[10_240];
        rnd.NextBytes(large);
        TestRoundTrip(large);

        Console.WriteLine("All HuffmanCodec unit tests passed.");
    }

    static void Main()
    {
        RunAllTests();
    }
}
