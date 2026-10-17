using System;
using System.Collections.Generic;

namespace CYKParserDemo
{
    // Represents a Context-Free Grammar in Chomsky Normal Form.
    public sealed class Grammar
    {
        public string StartSymbol { get; }

        // (B, C) -> {A | A -> B C}
        private readonly Dictionary<(string, string), HashSet<string>> _binaryRules
            = new Dictionary<(string, string), HashSet<string>>();

        // a -> {A | A -> a}
        private readonly Dictionary<string, HashSet<string>> _terminalRules
            = new Dictionary<string, HashSet<string>>();

        public Grammar(string startSymbol)
        {
            if (string.IsNullOrWhiteSpace(startSymbol))
                throw new ArgumentException("Start symbol must be a non‑empty string.", nameof(startSymbol));
            StartSymbol = startSymbol;
        }

        // Adds a terminal production: A -> a
        public void AddRule(string left, string terminal)
        {
            ValidateNonTerminal(left);
            if (string.IsNullOrEmpty(terminal))
                throw new ArgumentException("Terminal must be a non‑empty string.", nameof(terminal));

            if (!_terminalRules.TryGetValue(terminal, out var set))
            {
                set = new HashSet<string>();
                _terminalRules[terminal] = set;
            }
            set.Add(left);
        }

        // Adds a binary production: A -> B C
        public void AddRule(string left, string right1, string right2)
        {
            ValidateNonTerminal(left);
            ValidateNonTerminal(right1);
            ValidateNonTerminal(right2);

            var key = (right1, right2);
            if (!_binaryRules.TryGetValue(key, out var set))
            {
                set = new HashSet<string>();
                _binaryRules[key] = set;
            }
            set.Add(left);
        }

        // Retrieves all non‑terminals that produce the given terminal.
        internal HashSet<string> GetNonTerminalsForTerminal(string terminal)
        {
            if (_terminalRules.TryGetValue(terminal, out var set))
                return new HashSet<string>(set);
            return new HashSet<string>();
        }

        // Retrieves all non‑terminals that produce the given pair (B, C).
        internal HashSet<string> GetNonTerminalsForPair(string left, string right)
        {
            var key = (left, right);
            if (_binaryRules.TryGetValue(key, out var set))
                return new HashSet<string>(set);
            return new HashSet<string>();
        }

        private static void ValidateNonTerminal(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Non‑terminal symbol must be a non‑empty string.", nameof(symbol));
        }
    }

    // Implements the CYK parsing algorithm.
    public sealed class CYKParser
    {
        private readonly Grammar _grammar;

        public CYKParser(Grammar grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        // Returns true iff the input string belongs to the language of the grammar.
        public bool Parse(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            int n = input.Length;
            if (n == 0)
                return false; // CNF without epsilon productions.

            // table[i, j] holds the set of non‑terminals that derive substring input[i..j] (inclusive).
            var table = new HashSet<string>[n, n];

            // Length‑1 substrings (terminals).
            for (int i = 0; i < n; i++)
            {
                string symbol = input[i].ToString();
                table[i, i] = _grammar.GetNonTerminalsForTerminal(symbol);
            }

            // Substrings of length >= 2.
            for (int length = 2; length <= n; length++)
            {
                for (int i = 0; i <= n - length; i++)
                {
                    int j = i + length - 1;
                    var cellSet = new HashSet<string>();

                    for (int split = i; split < j; split++)
                    {
                        var leftSet = table[i, split];
                        var rightSet = table[split + 1, j];

                        if (leftSet == null || rightSet == null)
                            continue;

                        foreach (var B in leftSet)
                        {
                            foreach (var C in rightSet)
                            {
                                var producers = _grammar.GetNonTerminalsForPair(B, C);
                                if (producers.Count > 0)
                                    cellSet.UnionWith(producers);
                            }
                        }
                    }

                    table[i, j] = cellSet;
                }
            }

            // Acceptance test: does the start symbol derive the whole string?
            var finalSet = table[0, n - 1];
            return finalSet != null && finalSet.Contains(_grammar.StartSymbol);
        }
    }
}
