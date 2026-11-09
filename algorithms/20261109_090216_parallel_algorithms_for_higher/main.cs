using System;
using TemporalHIndexNS;

class Program
{
    static void Main()
    {
        RunAllTests();
        Console.WriteLine("All tests passed.");
    }

    static void RunAllTests()
    {
        TestComputeHIndexBasic();
        TestComputeTemporalHIndicesSimple();
        TestHigherOrderTemporalHIndex();
        TestEmptyAndSinglePaperCases();
        TestParallelDeterminism();
    }

    static void Assert(bool condition, string message = "Assertion failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void TestComputeHIndexBasic()
    {
        Assert(TemporalHIndex.ComputeHIndex(new int[] { }) == 0, "Empty array H-index");
        Assert(TemporalHIndex.ComputeHIndex(new[] { 0, 0, 0 }) == 0, "All zeros H-index");
        Assert(TemporalHIndex.ComputeHIndex(new[] { 1, 1, 1 }) == 1, "All ones H-index");
        Assert(TemporalHIndex.ComputeHIndex(new[] { 3, 0, 6, 1, 5 }) == 3, "Classic example");
        Assert(TemporalHIndex.ComputeHIndex(new[] { 10, 8, 5, 4, 3 }) == 4, "Descending order");
    }

    static void TestComputeTemporalHIndicesSimple()
    {
        // Two papers over three years.
        // Paper 0 citations: [0,1,2]
        // Paper 1 citations: [0,0,1]
        int[][] data = new[]
        {
            new[] {0,1,2},
            new[] {0,0,1}
        };
        int[] expected = new[] {0,1,1};
        int[] result = TemporalHIndex.ComputeTemporalHIndices(data);
        Assert(result.Length == expected.Length, "Temporal length mismatch");
        for (int i = 0; i < expected.Length; i++)
            Assert(result[i] == expected[i], $"Temporal H-index year {i}");
    }

    static void TestHigherOrderTemporalHIndex()
    {
        // Example from literature: three years, three papers.
        // Year 0 citations: [0,0,0] -> H=0
        // Year 1 citations: [1,2,2] -> H=2
        // Year 2 citations: [3,3,1] -> H=2
        // Temporal sequence = [0,2,2] -> higher‑order H = 2
        int[][] data = new[]
        {
            new[] {0,1,3},
            new[] {0,2,3},
            new[] {0,2,1}
        };
        int higher = TemporalHIndex.ComputeHigherOrderTemporalHIndex(data);
        Assert(higher == 2, "Higher‑order temporal H-index");
    }

    static void TestEmptyAndSinglePaperCases()
    {
        // No papers.
        int[][] empty = new int[0][];
        Assert(TemporalHIndex.ComputeTemporalHIndices(empty).Length == 0, "Empty input temporal length");
        Assert(TemporalHIndex.ComputeHigherOrderTemporalHIndex(empty) == 0, "Empty input higher order");

        // Single paper over four years.
        int[][] single = new[]
        {
            new[] {0,1,3,6}
        };
        int[] temporal = TemporalHIndex.ComputeTemporalHIndices(single);
        // Expected temporal H: each year only one paper, so H = min(citations,1) => 0,1,1,1
        int[] expectedTemporal = new[] {0,1,1,1};
        Assert(temporal.Length == expectedTemporal.Length, "Single paper length");
        for (int i = 0; i < temporal.Length; i++)
            Assert(temporal[i] == expectedTemporal[i], $"Single paper year {i}");

        int higher = TemporalHIndex.ComputeHigherOrderTemporalHIndex(single);
        // Temporal sequence [0,1,1,1] => H = 1
        Assert(higher == 1, "Single paper higher order");
    }

    static void TestParallelDeterminism()
    {
        // Stress test with many years to ensure parallel loop yields deterministic result.
        const int papers = 50;
        const int years = 200;
        var rnd = new Random(42);
        int[][] data = new int[papers][];
        for (int p = 0; p < papers; p++)
        {
            data[p] = new int[years];
            int cumulative = 0;
            for (int y = 0; y < years; y++)
            {
                cumulative += rnd.Next(0, 3); // small increments
                data[p][y] = cumulative;
            }
        }

        int[] firstRun = TemporalHIndex.ComputeTemporalHIndices(data);
        int[] secondRun = TemporalHIndex.ComputeTemporalHIndices(data);
        Assert(firstRun.Length == secondRun.Length, "Determinism length");
        for (int i = 0; i < firstRun.Length; i++)
            Assert(firstRun[i] == secondRun[i], $"Determinism mismatch at year {i}");
    }
}
