using System;
using System.Collections.Generic;

namespace SuffixAutomatonDemo
{
    public sealed class SuffixAutomaton
    {
        private readonly List<State> _states = new List<State>();
        private int _last; // index of the state representing the whole string processed so far

        public SuffixAutomaton(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            Build(text);
        }

        private void Build(string s)
        {
            // Create initial state
            _states.Add(new State(0, -1));
            _last = 0;

            foreach (char c in s)
            {
                Extend(c);
            }
        }

        private void Extend(char c)
        {
            int cur = _states.Count;
            _states.Add(new State(_states[_last].Length + 1, 0));
            int p = _last;

            // Propagate transition for character c
            while (p != -1 && !_states[p].Next.ContainsKey(c))
            {
                _states[p].Next[c] = cur;
                p = _states[p].Link;
            }

            if (p == -1)
            {
                _states[cur].Link = 0;
            }
            else
            {
                int q = _states[p].Next[c];
                if (_states[p].Length + 1 == _states[q].Length)
                {
                    _states[cur].Link = q;
                }
                else
                {
                    // Clone state q
                    int clone = _states.Count;
                    var clonedState = new State(_states[p].Length + 1, _states[q].Link);
                    foreach (var kv in _states[q].Next)
                    {
                        clonedState.Next[kv.Key] = kv.Value;
                    }
                    _states.Add(clonedState);

                    // Redirect links
                    while (p != -1 && _states[p].Next[c] == q)
                    {
                        _states[p].Next[c] = clone;
                        p = _states[p].Link;
                    }

                    _states[q].Link = clone;
                    _states[cur].Link = clone;
                }
            }

            _last = cur;
        }

        /// <summary>
        /// Determines whether the given pattern occurs as a substring of the text used to build the automaton.
        /// </summary>
        /// <param name="pattern">Pattern to search for. Empty string is always considered a substring.</param>
        /// <returns>True if pattern is a substring; otherwise false.</returns>
        public bool Contains(string pattern)
        {
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));
            if (pattern.Length == 0) return true; // empty string is a substring of any string

            int v = 0; // start at the initial state
            foreach (char c in pattern)
            {
                if (_states[v].Next.TryGetValue(c, out int next))
                {
                    v = next;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
