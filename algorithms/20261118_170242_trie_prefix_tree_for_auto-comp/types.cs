using System.Collections.Generic;

namespace AutoCompleteTrie
{
    // Represents a node in the Trie.
    internal sealed class TrieNode
    {
        // Children keyed by character.
        public readonly Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();

        // Indicates whether this node terminates a valid word.
        public bool IsWord { get; set; }

        // Frequency count for the word ending at this node.
        public int Frequency { get; set; }

        // The accumulated prefix up to this node (used for result reconstruction).
        public string Prefix { get; }

        public TrieNode(string prefix)
        {
            Prefix = prefix;
        }
    }

    // Result container for auto‑completion suggestions.
    public sealed class Suggestion
    {
        public string Word { get; }
        public int Frequency { get; }

        public Suggestion(string word, int frequency)
        {
            Word = word;
            Frequency = frequency;
        }
    }
}
