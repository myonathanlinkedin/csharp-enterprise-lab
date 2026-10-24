using System;
using System.Collections.Generic;

namespace ForkTrace
{
    /// <summary>
    /// Represents a directed graph where vertices are identified by integers.
    /// Provides functionality to trace back all possible source vertices
    /// (vertices with no incoming edges) that can reach a given target.
    /// </summary>
    public sealed class Graph
    {
        // Outgoing adjacency list: from -> list of to
        private readonly Dictionary<int, List<int>> _outgoing = new();

        // Incoming adjacency list: to -> list of from
        private readonly Dictionary<int, List<int>> _incoming = new();

        /// <summary>
        /// Adds a directed edge from <paramref name="from"/> to <paramref name="to"/>.
        /// Vertices are created implicitly if they do not exist.
        /// </summary>
        public void AddEdge(int from, int to)
        {
            if (!_outgoing.TryGetValue(from, out var outList))
            {
                outList = new List<int>();
                _outgoing[from] = outList;
            }
            outList.Add(to);

            if (!_incoming.TryGetValue(to, out var inList))
            {
                inList = new List<int>();
                _incoming[to] = inList;
            }
            inList.Add(from);

            // Ensure both vertices appear in the dictionaries even if they have no edges of a given direction.
            _outgoing.TryAdd(to, new List<int>());
            _incoming.TryAdd(from, new List<int>());
        }

        /// <summary>
        /// Returns all source vertices (vertices with zero incoming edges) that can reach <paramref name="target"/>
        /// by following directed edges forward. The search is performed on the reversed graph.
        /// </summary>
        /// <exception cref="KeyNotFoundException">If <paramref name="target"/> does not exist in the graph.</exception>
        public IReadOnlyCollection<int> GetSources(int target)
        {
            if (!_outgoing.ContainsKey(target) && !_incoming.ContainsKey(target))
                throw new KeyNotFoundException($"Vertex {target} is not present in the graph.");

            var visited = new HashSet<int>();
            var sources = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(target);
            visited.Add(target);

            while (stack.Count > 0)
            {
                int current = stack.Pop();

                // If current has no incoming edges, it is a source.
                if (!_incoming.TryGetValue(current, out var preds) || preds.Count == 0)
                {
                    sources.Add(current);
                    continue;
                }

                foreach (int pred in preds)
                {
                    if (visited.Add(pred))
                        stack.Push(pred);
                }
            }

            return sources;
        }

        /// <summary>
        /// Returns a read‑only collection of all vertices currently stored in the graph.
        /// </summary>
        public IReadOnlyCollection<int> Vertices
        {
            get
            {
                var set = new HashSet<int>(_outgoing.Keys);
                set.UnionWith(_incoming.Keys);
                return set;
            }
        }
    }
}
