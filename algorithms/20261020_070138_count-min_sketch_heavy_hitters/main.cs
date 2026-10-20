using System;
using System.Collections.Generic;
using System.Diagnostics;
using CountMinSketchNS;

namespace CountMinSketchDemo
{
    internal static class Program
    {
        private static void Main()
        {
            // Run all verification tests. Any failed assertion will throw.
            TestBasicCounting();
            TestOverestimationBound();
            TestHeavyHittersExtraction();
            TestMergeOperation();
            Console.WriteLine("All Count‑Min Sketch tests passed.");
        }

        private static void TestBasicCounting()
        {
            var sketch = new CountMinSketch<string>(depth: 5, width: 1024);
            sketch.Add("apple", 3);
            sketch.Add("banana", 2);
            sketch.Add("apple", 2); // total apple = 5

            long appleEst = sketch.Estimate("apple");
            long bananaEst = sketch.Estimate("banana");
            long orangeEst = sketch.Estimate("orange"); // never added

            Debug.Assert(appleEst == 5, $"Apple count expected 5, got {appleEst}");
            Debug.Assert(bananaEst == 2, $"Banana count expected 2, got {bananaEst}");
            Debug.Assert(orangeEst == 0, $"Orange count expected 0, got {orangeEst}");
        }

        private static void TestOverestimationBound()
        {
            // With depth d and width w, the error is at most totalCount / w with probability 1.
            const int depth = 4;
            const int width = 128;
            var sketch = new CountMinSketch<int>(depth, width);
            const int totalAdds = 10_000;
            const int target = 42;

            // Add many distinct items to increase total count.
            for (int i = 0; i < totalAdds; i++)
                sketch.Add(i, 1);

            // Add the target item many times.
            const int targetAdds = 1234;
            sketch.Add(target, targetAdds);

            long estimate = sketch.Estimate(target);
            long errorBound = totalAdds / width + 1; // +1 for integer division rounding

            Debug.Assert(estimate >= targetAdds, "Estimate must not be below true count.");
            Debug.Assert(estimate <= targetAdds + errorBound,
                $"Estimate {estimate} exceeds allowed error bound {targetAdds + errorBound}");
        }

        private static void TestHeavyHittersExtraction()
        {
            var sketch = new CountMinSketch<char>(depth: 6, width: 256);
            var items = new[] { 'a', 'b', 'c', 'd', 'e' };
            var frequencies = new Dictionary<char, int>
            {
                ['a'] = 500,
                ['b'] = 300,
                ['c'] = 150,
                ['d'] = 80,
                ['e'] = 20
            };

            foreach (var kvp in frequencies)
                sketch.Add(kvp.Key, kvp.Value);

            // Heavy hitters threshold = 100
            IList<char> heavy = sketch.GetHeavyHitters(items, threshold: 100);
            var expected = new HashSet<char> { 'a', 'b', 'c' };
            Debug.Assert(heavy.Count == expected.Count, $"Expected {expected.Count} heavy hitters, got {heavy.Count}");
            foreach (var h in heavy)
                Debug.Assert(expected.Contains(h), $"Unexpected heavy hitter {h}");
        }

        private static void TestMergeOperation()
        {
            var sketch1 = new CountMinSketch<string>(depth: 3, width: 512);
            var sketch2 = new CountMinSketch<string>(depth: 3, width: 512);

            sketch1.Add("x", 7);
            sketch1.Add("y", 3);
            sketch2.Add("x", 2);
            sketch2.Add("z", 5);

            sketch1.Merge(sketch2);

            Debug.Assert(sketch1.Estimate("x") == 9, $"Merged x count expected 9, got {sketch1.Estimate("x")}");
            Debug.Assert(sketch1.Estimate("y") == 3, $"Merged y count expected 3, got {sketch1.Estimate("y")}");
            Debug.Assert(sketch1.Estimate("z") == 5, $"Merged z count expected 5, got {sketch1.Estimate("z")}");
        }
    }
}
