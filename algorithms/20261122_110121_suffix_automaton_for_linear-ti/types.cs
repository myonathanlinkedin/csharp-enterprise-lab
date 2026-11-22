using System.Collections.Generic;

namespace SuffixAutomatonDemo
{
    internal sealed class State
    {
        public int Length { get; set; }          // Max length of strings in this equivalence class
        public int Link { get; set; }            // Suffix link
        public Dictionary<char, int> Next { get; } = new Dictionary<char, int>();

        public State(int length = 0, int link = -1)
        {
            Length = length;
            Link = link;
        }
    }
}
