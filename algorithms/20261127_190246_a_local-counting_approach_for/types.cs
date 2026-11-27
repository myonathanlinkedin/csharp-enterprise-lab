using System.Collections.Generic;

namespace LocalCountingPathDetection
{
    /// <summary>
    /// Simple undirected graph represented by an adjacency list.
    /// Vertices are identified by non‑negative integer IDs.
    /// </summary>
    public sealed class Graph
    {
        private readonly Dictionary<int, HashSet<int>> _adjacency = new();

        /// <summary>
        /// Adds an undirected edge between <paramref name="u"/> and <paramref name="v"/>.
        /// Self‑loops are ignored.
        /// </summary>
        public void AddEdge(int u, int v)
        {
            if (u == v) return;

            if (!_adjacency.TryGetValue(u, out var setU))
            {
                setU = new HashSet<int>();
                _adjacency[u] = setU;
            }
            setU.Add(v);

            if (!_adjacency.TryGetValue(v, out var setV))
            {
                setV = new HashSet<int>();
                _adjacency[v] = setV;
            }
            setV.Add(u);
        }

        /// <summary>
        /// Returns all vertices present in the graph.
        /// </summary>
        public IEnumerable<int> Vertices => _adjacency.Keys;

        /// <summary>
        /// Returns the neighbor set of <paramref name="v"/>.
        /// If the vertex does not exist, returns an empty set.
        /// </summary>
        public IReadOnlyCollection<int> GetNeighbors(int v)
        {
            return _adjacency.TryGetValue(v, out var set) ? (IReadOnlyCollection<int>)set : System.Array.Empty<int>();
        }

        /// <summary>
        /// Returns true if the graph contains the vertex <paramref name="v"/>.
        /// </summary>
        public bool ContainsVertex(int v) => _adjacency.ContainsKey(v);
    }
}
