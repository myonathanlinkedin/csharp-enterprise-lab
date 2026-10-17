using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CYKParserDemo
{
    internal static class Test
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        // Grammar for the language { a^n b^n | n ≥ 1 } in CNF.
        // Non‑terminals: S, A, B, X
        // Terminals: a, b
        // Productions:
        //   S -> A B
        //   S -> A X
        //   X -> S B
        //   A -> a
        //   B -> b
        private static Grammar BuildAnBnGrammar()
        {
            var g = new Grammar("S");
            g.AddRule("S", "A", "B");
            g.AddRule("S", "A", "X");
            g.AddRule("X", "S", "B");
            g.AddRule("A", "a");
            g.AddRule("B", "b");
            return g;
        }

        // Grammar that generates exactly the string "ab".
        private static Grammar BuildExactAbGrammar()
        {
            var g = new Grammar("S");
            g.AddRule("S", "A", "B");
            g.AddRule("A", "a");
            g.AddRule("B", "b");
            return g;
        }

        public static void RunAll()
        {
            // Test 1: a^n b^n grammar
            var grammar1 = BuildAnBnGrammar();
            var parser1 = new CYKParser(grammar1);

            Assert(parser1.Parse("ab") == true, "ab should be accepted");
            Assert(parser1.Parse("aabb") == true, "aabb should be accepted");
            Assert(parser1.Parse("aaabbb") == true, "aaabbb should be accepted");
            Assert(parser1.Parse("aab") == false, "aab should be rejected");
            Assert(parser1.Parse("abb") == false, "abb should be rejected");
            Assert(parser1.Parse("") == false, "empty string should be rejected");

            // Test 2: exact "ab" grammar
            var grammar2 = BuildExactAbGrammar();
            var parser2 = new CYKParser(grammar2);

            Assert(parser2.Parse("ab") == true, "Exact grammar: ab accepted");
            Assert(parser2.Parse("a") == false, "Exact grammar: a rejected");
            Assert(parser2.Parse("b") == false, "Exact grammar: b rejected");
            Assert(parser2.Parse("abc") == false, "Exact grammar: abc rejected");
            Assert(parser2.Parse("") == false, "Exact grammar: empty rejected");

            // Test 3: Stress test with longer string
            string longString = new string('a', 10) + new string('b', 10);
            Assert(parser1.Parse(longString) == true, "Long a^10 b^10 should be accepted");

            // Test 4: Invalid characters
            Assert(parser1.Parse("aacbb") == false, "String with invalid order should be rejected");

            Console.WriteLine("All CYK parser tests passed.");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            Test.RunAll();
        }
    }
}
