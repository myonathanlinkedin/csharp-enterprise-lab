using System;
using System.Collections.Generic;
using CykParser;

namespace CykParser
{
    public static class TestRunner
    {
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception("Assertion failed: " + message);
        }

        public static void RunAll()
        {
            TestEvenA();
            TestABBA();
            TestFalseCases();
            Benchmark();
            Console.WriteLine("All tests passed.");
        }

        private static void TestEvenA()
        {
            var grammar = new Grammar(
                new[] { "S", "A" },
                new[] { "a" },
                "S",
                new[]
                {
                    new Production("S", "A", "A"),
                    new Production("A", "a")
                });

            Assert(Cyk.Parse("aa", grammar), "aa should be accepted");
            Assert(Cyk.Parse("aaaa", grammar), "aaaa should be accepted");
            Assert(!Cyk.Parse("a", grammar), "a should be rejected");
            Assert(!Cyk.Parse("aaa", grammar), "aaa should be rejected");
        }

        private static void TestABBA()
        {
            var grammar = new Grammar(
                new[] { "S", "A", "B" },
                new[] { "a", "b" },
                "S",
                new[]
                {
                    new Production("S", "A", "B"),
                    new Production("S", "B", "A"),
                    new Production("A", "a"),
                    new Production("B", "b")
                });

            Assert(Cyk.Parse("ab", grammar), "ab should be accepted");
            Assert(Cyk.Parse("ba", grammar), "ba should be accepted");
            Assert(!Cyk.Parse("aa", grammar), "aa should be rejected");
            Assert(!Cyk.Parse("bb", grammar), "bb should be rejected");
        }

        private static void TestFalseCases()
        {
            var grammar = new Grammar(
                new[] { "S", "A" },
                new[] { "a" },
                "S",
                new[]
                {
                    new Production("S", "A", "A"),
                    new Production("A", "a")
                });

            Assert(!Cyk.Parse("", grammar), "empty string should be rejected");
            Assert(!Cyk.Parse("b", grammar), "b should be rejected");
        }

        private static void Benchmark()
        {
            var grammar = new Grammar(
                new[] { "S", "A" },
                new[] { "a" },
                "S",
                new[]
                {
                    new Production("S", "A", "A"),
                    new Production("A", "a")
                });

            string input = new string('a', 200); // 200 a's
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool result = Cyk.Parse(input, grammar);
            sw.Stop();
            Assert(result, "Benchmark input should be accepted");
            Console.WriteLine($"Benchmark: parsed {input.Length} 'a's in {sw.ElapsedMilliseconds} ms");
        }
    }

    public class Program
    {
        public static void Main()
        {
            TestRunner.RunAll();
        }
    }
}
