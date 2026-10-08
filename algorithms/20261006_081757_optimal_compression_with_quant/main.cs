using System;
using System.Collections.Generic;
using System.Diagnostics;
using QuantumCompression;

namespace QuantumCompression
{
    public static class Program
    {
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                _testsPassed++;
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"[FAIL] {testName}: Expected {expected}, got {actual}");
            }
        }

        private static void AssertTrue(bool condition, string testName)
        {
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"[FAIL] {testName}");
            }
        }

        public static int Main(string[] args)
        {
            Console.WriteLine("=== Quantum Compression Unit Tests ===");

            TestSingleElement();
            TestTwoElements();
            TestSortedArray();
            TestReverseSortedArray();
            TestRandomArray();
            TestLargeArray();
            TestBoundaryConditions();
            TestInvalidInputs();
            TestPartitionValidity();
            TestCompressionRoundTrip();

            Console.WriteLine($"\n=== Results: {_testsPassed} passed, {_testsFailed} failed ===");
            return _testsFailed == 0 ? 0 : 1;
        }

        private static void TestSingleElement()
        {
            var data = new[] { 42 };
            var qr = new QuantumRetriever(data, 1);
            AssertEqual(0, qr.GetOptimalCost(), "Single element cost is 0");
            var partitions = qr.GetOptimalPartitions();
            AssertEqual(1, partitions.Length, "Single element has 1 partition");
            AssertEqual(0, partitions[0], "Single element partition starts at 0");
        }

        private static void TestTwoElements()
        {
            var data = new[] { 10, 20 };
            var qr = new QuantumRetriever(data, 1);
            AssertEqual(10, qr.GetOptimalCost(), "Two elements, k=1 cost is 10");

            var qr2 = new QuantumRetriever(data, 2);
            AssertEqual(0, qr2.GetOptimalCost(), "Two elements, k=2 cost is 0");
        }

        private static void TestSortedArray()
        {
            var data = new[] { 1, 2, 3, 4, 5 };
            var qr = new QuantumRetriever(data, 2);
            AssertEqual(2, qr.GetOptimalCost(), "Sorted array, k=2 cost is 2");

            var qr3 = new QuantumRetriever(data, 3);
            AssertEqual(1, qr3.GetOptimalCost(), "Sorted array, k=3 cost is 1");
        }

        private static void TestReverseSortedArray()
        {
            var data = new[] { 5, 4, 3, 2, 1 };
            var qr = new QuantumRetriever(data, 2);
            AssertEqual(2, qr.GetOptimalCost(), "Reverse sorted, k=2 cost is 2");
        }

        private static void TestRandomArray()
        {
            var rng = new Random(12345);
            var data = new int[10];
            for (int i = 0; i < 10; i++)
                data[i] = rng.Next(1, 100);

            var qr = new QuantumRetriever(data, 3);
            int cost = qr.GetOptimalCost();
            AssertTrue(cost >= 0, "Random array cost is non-negative");

            var partitions = qr.GetOptimalPartitions();
            AssertEqual(3, partitions.Length, "Random array has 3 partitions");
        }

        private static void TestLargeArray()
        {
            var rng = new Random(99999);
            var data = new int[100];
            for (int i = 0; i < 100; i++)
                data[i] = rng.Next(1, 1000);

            var sw = Stopwatch.StartNew();
            var qr = new QuantumRetriever(data, 10);
            sw.Stop();

            AssertTrue(sw.ElapsedMilliseconds < 1000, "Large array completes in < 1s");
            AssertTrue(qr.GetOptimalCost() >= 0, "Large array cost is non-negative");
        }

        private static void TestBoundaryConditions()
        {
            var data = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var qr1 = new QuantumRetriever(data, 1);
            AssertEqual(9, qr1.GetOptimalCost(), "k=1 cost is max-min");

            var qr10 = new QuantumRetriever(data, 10);
            AssertEqual(0, qr10.GetOptimalCost(), "k=n cost is 0");
        }

        private static void TestInvalidInputs()
        {
            bool threwNull = false;
            try { new QuantumRetriever(null, 1); }
            catch (ArgumentNullException) { threwNull = true; }
            AssertTrue(threwNull, "Null data throws ArgumentNullException");

            bool threwArg = false;
            try { new QuantumRetriever(new[] { 1, 2 }, 0); }
            catch (ArgumentOutOfRangeException) { threwArg = true; }
            AssertTrue(threwArg, "k=0 throws ArgumentOutOfRangeException");

            bool threwArg2 = false;
            try { new QuantumRetriever(new[] { 1, 2 }, 3); }
            catch (ArgumentOutOfRangeException) { threwArg2 = true; }
            AssertTrue(threwArg2, "k>n throws ArgumentOutOfRangeException");
        }

        private static void TestPartitionValidity()
        {
            var data = new[] { 5, 1, 8, 3, 7, 2, 9, 4, 6, 10 };
            var qr = new QuantumRetriever(data, 4);
            var partitions = qr.GetOptimalPartitions();

            AssertEqual(4, partitions.Length, "Partitions count matches k");
            AssertEqual(0, partitions[0], "First partition starts at 0");

            for (int i = 1; i < partitions.Length; i++)
            {
                AssertTrue(partitions[i] > partitions[i - 1], $"Partition {i} is increasing");
            }
        }

        private static void TestCompressionRoundTrip()
        {
            var data = new[] { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 };
            var qr = new QuantumRetriever(data, 3);
            var compressed = qr.GetCompressedRepresentation();

            AssertEqual(6, compressed.Length, "Compressed representation has 2*k values");

            for (int i = 0; i < compressed.Length; i += 2)
            {
                AssertTrue(compressed[i] <= compressed[i + 1], $"Min <= Max at index {i}");
            }
        }
    }
}
