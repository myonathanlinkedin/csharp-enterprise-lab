using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoCompleteTrie
{
    // Core Trie implementation with frequency‑aware auto‑completion.
    public sealed class Trie
    {
        private readonly TrieNode _root = new TrieNode(string.Empty);

        // Inserts a word or increments its frequency by the supplied delta (default 1).
        public void Insert(string word, int delta = 1)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            if (delta <= 0) throw new ArgumentOutOfRangeException(nameof(delta), "Delta must be positive.");

            var node = _root;
            foreach (char ch in word)
            {
                if (!node.Children.TryGetValue(ch, out TrieNode child))
                {
                    child = new TrieNode(node.Prefix + ch);
                    node.Children[ch] = child;
                }
                node = child;
            }

            node.IsWord = true;
            checked
            {
                node.Frequency += delta;
            }
        }

        // Retrieves up to 'k' suggestions for the given prefix, ordered by descending frequency
        // and then lexicographically.
        public IList<Suggestion> GetTopK(string prefix, int k)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "k must be positive.");

            var node = _root;
            foreach (char ch in prefix)
            {
                if (!node.Children.TryGetValue(ch, out node))
                {
                    // Prefix not present – return empty list.
                    return Array.Empty<Suggestion>();
                }
            }

            // Collect all words under this node.
            var candidates = new List<Suggestion>();
            Collect(node, candidates);

            // Order by frequency descending, then lexicographically.
            var ordered = candidates
                .OrderByDescending(s => s.Frequency)
                .ThenBy(s => s.Word, StringComparer.Ordinal)
                .Take(k)
                .ToList();

            return ordered;
        }

        // Depth‑first collection of words starting from 'node'.
        private static void Collect(TrieNode node, IList<Suggestion> accumulator)
        {
            if (node.IsWord)
            {
                accumulator.Add(new Suggestion(node.Prefix, node.Frequency));
            }

            foreach (var child in node.Children.Values)
            {
                Collect(child, accumulator);
            }
        }
    }
}
