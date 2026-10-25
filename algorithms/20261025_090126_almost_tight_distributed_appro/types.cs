using System.Collections.Generic;

namespace DominationApprox
{
    /// <summary>
    /// Represents an undirected simple graph using adjacency sets.
    /// </summary>
    public sealed class Graph
    {
        private readonly Dictionary<int, HashSet<int>> _adjacency = new();

        /// <summary>
        /// Adds a vertex to the graph. If the vertex already exists, the call is ignored.
        /// </summary>
        public void AddVertex(int v)
        {
            if (!_adjacency.ContainsKey(v))
            {
                _adjacency[v] = new HashSet<int>();
            }
        }

        /// <summary>
        /// Adds an undirected edge (u, v). Vertices are created automatically if missing.
        /// Self‑loops are ignored.
        /// </summary>
        public void AddEdge(int u, int v)
        {
            if (u == v) return; // ignore self‑loops

            AddVertex(u);
            AddVertex(v);
            _adjacency[u].Add(v);
            _adjacency[v].Add(u);
        }

        /// <summary>
        /// Returns all vertices present in the graph.
        /// </summary>
        public IEnumerable<int> Vertices => _adjacency.Keys;

        /// <summary>
        /// Returns the neighbor set of a given vertex. If the vertex does not exist, an empty set is returned.
        /// </summary>
        public IReadOnlyCollection<int> Neighbors(int v)
        {
            return _adjacency.TryGetValue(v, out var set) ? (IReadOnlyCollection<int>)set : new HashSet<int>();
        }

        /// <summary>
        /// Returns the degree of a vertex (number of incident edges).
        /// </summary>
        public int Degree(int v)
        {
            return _adjacency.TryGetValue(v, out var set) ? set.Count : 0;
        }

        /// <summary>
        /// Returns true if the graph contains no vertices.
        /// </summary>
        public bool IsEmpty => _adjacency.Count == 0;
    }
}
