using System;
using System.Collections.Generic;

namespace CykParser
{
    public class Grammar
    {
        public HashSet<string> NonTerminals { get; }
        public HashSet<string> Terminals { get; }
        public string StartSymbol { get; }
        public List<Production> Productions { get; }

        public Grammar(IEnumerable<string> nonTerminals, IEnumerable<string> terminals, string startSymbol, IEnumerable<Production> productions)
        {
            NonTerminals = new HashSet<string>(nonTerminals);
            Terminals = new HashSet<string>(terminals);
            StartSymbol = startSymbol;
            Productions = new List<Production>(productions);
        }
    }

    public class Production
    {
        public string Left { get; }
        public string[] Right { get; } // length 1 or 2

        public Production(string left, params string[] right)
        {
            Left = left;
            Right = right;
        }
    }

    public static class Cyk
    {
        public static bool Parse(string input, Grammar grammar)
        {
            if (string.IsNullOrEmpty(input))
                return false; // CNF cannot derive epsilon

            int n = input.Length;
            var table = new HashSet<string>[n, n];

            // Initialize table for substrings of length 1
            for (int i = 0; i < n; i++)
            {
                table[i, 0] = new HashSet<string>();
                foreach (var prod in grammar.Productions)
                {
                    if (prod.Right.Length == 1 && prod.Right[0] == input[i].ToString())
                    {
                        table[i, 0].Add(prod.Left);
                    }
                }
            }

            // Build table for substrings of length >1
            for (int l = 2; l <= n; l++) // length of span
            {
                for (int i = 0; i <= n - l; i++) // start index
                {
                    table[i, l - 1] = new HashSet<string>();
                    for (int s = 1; s < l; s++) // split position
                    {
                        var leftSet = table[i, s - 1];
                        var rightSet = table[i + s, l - s - 1];
                        foreach (var prod in grammar.Productions)
                        {
                            if (prod.Right.Length == 2)
                            {
                                if (leftSet.Contains(prod.Right[0]) && rightSet.Contains(prod.Right[1]))
                                {
                                    table[i, l - 1].Add(prod.Left);
                                }
                            }
                        }
                    }
                }
            }

            return table[0, n - 1].Contains(grammar.StartSymbol);
        }
    }
}
