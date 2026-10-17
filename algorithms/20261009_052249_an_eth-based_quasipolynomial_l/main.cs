using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dualization
{
    internal static class TestHelper
    {
        public static void AssertTrue(bool condition, string message = "")
        {
            if (!condition)
                throw new InvalidOperationException("Assertion failed. " + message);
        }

        public static void AssertEqualSets(IReadOnlyCollection<HashSet<int>> expected, IReadOnlyCollection<HashSet<int>> actual, string testName)
        {
            // Normalize by sorting each set and then sorting the collection
            var exp = expected.Select(s => new SortedSet<int>(s)).OrderBy(s => string.Join(",", s)).ToList();
            var act = actual.Select(s => new SortedSet<int>(s)).OrderBy(s => string.Join(",", s)).ToList();

            AssertTrue(exp.Count == act.Count, $"{testName}: Expected {exp.Count} sets, got {act.Count}.");

            for (int i = 0; i < exp.Count; i++)
            {
                AssertTrue(exp[i].SetEquals(act[i]), $"{testName}: Set mismatch at index {i}. Expected {{{string.Join(",", exp[i])}}}, got {{{string.Join(",", act[i])}}}.");
            }
        }
    }

    internal class Program
    {
        private static void RunTests()
        {
            // Test 1: Simple hypergraph { {1,2}, {2,3} } => dual = { {2}, {1,3} }
            var hg1 = new Hypergraph();
            hg1.AddEdge(new[] { 1, 2 });
            hg1.AddEdge(new[] { 2, 3 });
            var dual1 = Dualizer.ComputeDual(hg1);
            var expected1 = new List<HashSet<int>>
            {
                new HashSet<int>{2},
                new HashSet<int>{1,3}
            };
            TestHelper.AssertEqualSets(expected1, dual1, "Test1");

            // Test 2: Empty hypergraph (no edges) => dual = { ∅ }
            var hg2 = new Hypergraph();
            var dual2 = Dualizer.ComputeDual(hg2);
            var expected2 = new List<HashSet<int>> { new HashSet<int>() };
            TestHelper.AssertEqualSets(expected2, dual2, "Test2");

            // Test 3: Hypergraph containing an empty edge => dual = ∅ (no transversal)
            var hg3 = new Hypergraph();
            hg3.AddEdge(new int[] { }); // empty edge
            var dual3 = Dualizer.ComputeDual(hg3);
            var expected3 = new List<HashSet<int>>(); // empty list
            TestHelper.AssertEqualSets(expected3, dual3, "Test3");

            // Test 4: Single edge {1,2,3} => dual = { {1}, {2}, {3} }
            var hg4 = new Hypergraph();
            hg4.AddEdge(new[] { 1, 2, 3 });
            var dual4 = Dualizer.ComputeDual(hg4);
            var expected4 = new List<HashSet<int>>
            {
                new HashSet<int>{1},
                new HashSet<int>{2},
                new HashSet<int>{3}
            };
            TestHelper.AssertEqualSets(expected4, dual4, "Test4");

            // Test 5: Two disjoint edges {1,2} and {3,4} => dual = { {1,3}, {1,4}, {2,3}, {2,4} }
            var hg5 = new Hypergraph();
            hg5.AddEdge(new[] { 1, 2 });
            hg5.AddEdge(new[] { 3, 4 });
            var dual5 = Dualizer.ComputeDual(hg5);
            var expected5 = new List<HashSet<int>>
            {
                new HashSet<int>{1,3},
                new HashSet<int>{1,4},
                new HashSet<int>{2,3},
                new HashSet<int>{2,4}
            };
            TestHelper.AssertEqualSets(expected5, dual5, "Test5");

            Console.WriteLine("All unit tests passed.");
        }

        private static void Demo()
        {
            var hg = new Hypergraph();
            hg.AddEdge(new[] { 1, 2, 5 });
            hg.AddEdge(new[] { 2, 3 });
            hg.AddEdge(new[] { 3, 4 });
            hg.AddEdge(new[] { 4, 5 });

            var dual = Dualizer.ComputeDual(hg);
            Console.WriteLine("Dual (minimal transversals):");
            foreach (var t in dual)
            {
                Console.WriteLine("{" + string.Join(", ", t.OrderBy(v => v)) + "}");
            }
        }

        static void Main()
        {
            RunTests();
            Demo();
        }
    }
}
