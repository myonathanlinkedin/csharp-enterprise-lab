using System;
using System.Collections.Generic;
using AutoCompleteTrie;

namespace AutoCompleteTrieDemo
{
    internal static class Program
    {
        private static void Main()
        {
            var trie = new Trie();

            // Basic insertion and frequency increment.
            trie.Insert("apple");
            trie.Insert("app");
            trie.Insert("application");
            trie.Insert("app"); // Increment frequency of "app" to 2.

            // Verify frequencies via suggestions.
            var suggestions = trie.GetTopK("app", 10);
            AssertEqual(3, suggestions.Count, "Expected three suggestions for prefix 'app'.");

            // Expected order: "app" (freq 2), "apple" (freq 1), "application" (freq 1)
            AssertEqual("app", suggestions[0].Word, "First suggestion should be 'app'.");
            AssertEqual(2, suggestions[0].Frequency, "'app' frequency should be 2.");
            AssertEqual("apple", suggestions[1].Word, "Second suggestion should be 'apple'.");
            AssertEqual("application", suggestions[2].Word, "Third suggestion should be 'application'.");

            // Edge case: prefix not present.
            var empty = trie.GetTopK("xyz", 5);
            AssertEqual(0, empty.Count, "No suggestions should be returned for unknown prefix.");

            // Edge case: limit k smaller than available suggestions.
            var limited = trie.GetTopK("app", 2);
            AssertEqual(2, limited.Count, "Should return only two suggestions when k=2.");
            AssertEqual("app", limited[0].Word);
            AssertEqual("apple", limited[1].Word);

            // Edge case: inserting words with shared prefixes.
            trie.Insert("banana");
            trie.Insert("band");
            trie.Insert("bandana");
            trie.Insert("band", 3); // Now "band" frequency = 4.

            var bandSuggestions = trie.GetTopK("ban", 5);
            AssertEqual(3, bandSuggestions.Count, "Three suggestions expected for prefix 'ban'.");
            AssertEqual("band", bandSuggestions[0].Word);
            AssertEqual(4, bandSuggestions[0].Frequency);
            AssertEqual("bandana", bandSuggestions[1].Word);
            AssertEqual("banana", bandSuggestions[2].Word);

            // Edge case: requesting more suggestions than exist.
            var many = trie.GetTopK("b", 10);
            AssertEqual(3, many.Count, "Only three suggestions exist for prefix 'b'.");

            // Stress test: large number of insertions.
            for (int i = 0; i < 1000; i++)
            {
                trie.Insert($"test{i}");
            }
            var testSuggestions = trie.GetTopK("test", 5);
            AssertEqual(5, testSuggestions.Count, "Should retrieve five suggestions for prefix 'test'.");

            // Verify deterministic ordering when frequencies tie.
            trie.Insert("alpha");
            trie.Insert("beta");
            var tie = trie.GetTopK("", 2);
            // Both have frequency 1; order should be lexical: "alpha" then "beta".
            AssertEqual("alpha", tie[0].Word);
            AssertEqual("beta", tie[1].Word);

            Console.WriteLine("All assertions passed.");
        }

        private static void AssertEqual<T>(T expected, T actual, string? message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"Assertion failed. Expected: {expected}, Actual: {actual}. {(message ?? string.Empty)}");
            }
        }
    }
}
