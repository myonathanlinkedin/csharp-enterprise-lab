using System;
using System.Linq;
using BarelyMonotone;

class Program
{
    static void Main()
    {
        var engine = new BarelyMonotoneConvolutionEngine();

        // Test 1: Simple increasing sequences
        var a1 = new Sequence(new int[] { 0, 1, 2 });
        var b1 = new Sequence(new int[] { 0, 1, 2 });
        var expected1 = new int[] { 0, 1, 2, 3, 4 };
        var result1 = engine.Convolve(a1, b1);
        AssertEqual(expected1, result1, "Test 1");

        // Test 2: Different values
        var a2 = new Sequence(new int[] { 1, 3, 5 });
        var b2 = new Sequence(new int[] { 2, 4, 6 });
        var expected2 = new int[] { 3, 5, 7, 9, 11 };
        var result2 = engine.Convolve(a2, b2);
        AssertEqual(expected2, result2, "Test 2");

        // Test 3: Negative values
        var a3 = new Sequence(new int[] { -5, -3, -1 });
        var b3 = new Sequence(new int[] { -4, -2, 0 });
        var expected3 = new int[] { -9, -7, -5, -3, -1 };
        var result3 = engine.Convolve(a3, b3);
        AssertEqual(expected3, result3, "Test 3");

        // Test 4: One empty sequence
        var a4 = new Sequence(new int[] { });
        var b4 = new Sequence(new int[] { 1, 2, 3 });
        var expected4 = Array.Empty<int>();
        var result4 = engine.Convolve(a4, b4);
        AssertEqual(expected4, result4, "Test 4");

        // Test 5: Random small sequences vs naive
        var rnd = new Random(42);
        for (int t = 0; t < 10; t++)
        {
            int lenA = rnd.Next(1, 10);
            int lenB = rnd.Next(1, 10);
            int[] arrA = Enumerable.Range(0, lenA).Select(_ => rnd.Next(-10, 10)).ToArray();
            int[] arrB = Enumerable.Range(0, lenB).Select(_ => rnd.Next(-10, 10)).ToArray();
            var seqA = new Sequence(arrA);
            var seqB = new Sequence(arrB);
            var naive = NaiveConvolution(arrA, arrB);
            var fast = engine.Convolve(seqA, seqB);
            AssertEqual(naive, fast, $"Random Test {t + 1}");
        }

        Console.WriteLine("All tests passed.");
    }

    static int[] NaiveConvolution(int[] a, int[] b)
    {
        int n = a.Length;
        int m = b.Length;
        if (n == 0 || m == 0) return Array.Empty<int>();
        int[] res = new int[n + m - 1];
        for (int i = 0; i < res.Length; i++) res[i] = int.MaxValue;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                int k = i + j;
                int val = a[i] + b[j];
                if (val < res[k]) res[k] = val;
            }
        }
        return res;
    }

    static void AssertEqual(int[] expected, int[] actual, string testName)
    {
        if (expected.Length != actual.Length)
        {
            throw new Exception($"{testName} failed: length mismatch. Expected {expected.Length}, got {actual.Length}");
        }
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new Exception($"{testName} failed at index {i}. Expected {expected[i]}, got {actual[i]}");
            }
        }
    }
}
