using System;
using System.Collections.Generic;

namespace KruskalMST
{
    public readonly struct Edge : IComparable<Edge>
    {
        public int Source { get; }
        public int Target { get; }
        public long Weight { get; }

        public Edge(int source, int target, long weight)
        {
            if (source < 0) throw new ArgumentOutOfRangeException(nameof(source));
            if (target < 0) throw new ArgumentOutOfRangeException(nameof(target));
            Source = source;
            Target = target;
            Weight = weight;
        }

        public int CompareTo(Edge other) => Weight.CompareTo(other.Weight);
    }

    internal sealed class DisjointSetUnion
    {
        private readonly int[] _parent;
        private readonly int[] _rank;

        public DisjointSetUnion(int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            _parent = new int[size];
            _rank = new int[size];
            for (int i = 0; i < size; i++) _parent[i] = i;
        }

        public int Find(int x)
        {
            if (x < 0 || x >= _parent.Length) throw new ArgumentOutOfRangeException(nameof(x));
            if (_parent[x] != x) _parent[x] = Find(_parent[x]);
            return _parent[x];
        }

        // Returns true if a union was performed (i.e., sets were different)
        public bool Union(int x, int y)
        {
            int xr = Find(x);
            int yr = Find(y);
            if (xr == yr) return false;

            if (_rank[xr] < _rank[yr])
                _parent[xr] = yr;
            else if (_rank[xr] > _rank[yr])
                _parent[yr] = xr;
            else
            {
                _parent[yr] = xr;
                _rank[xr]++;
            }
            return true;
        }
    }

    public static class KruskalAlgorithm
    {
        /// <summary>
        /// Computes a Minimum Spanning Tree (MST) using Kruskal's algorithm.
        /// </summary>
        /// <param name="vertexCount">Number of vertices in the graph (must be &gt; 0).</param>
        /// <param name="edges">All undirected edges of the graph.</param>
        /// <returns>A tuple containing the list of edges in the MST and its total weight.</returns>
        /// <exception cref="InvalidOperationException">If the input graph is disconnected.</exception>
        public static (List<Edge> mst, long totalWeight) ComputeMST(int vertexCount, List<Edge> edges)
        {
            if (vertexCount <= 0) throw new ArgumentOutOfRangeException(nameof(vertexCount));
            if (edges == null) throw new ArgumentNullException(nameof(edges));

            // Sort edges by weight (non‑decreasing)
            var sorted = new List<Edge>(edges);
            sorted.Sort();

            var dsu = new DisjointSetUnion(vertexCount);
            var mst = new List<Edge>(vertexCount - 1);
            long total = 0;

            foreach (var e in sorted)
            {
                if (dsu.Union(e.Source, e.Target))
                {
                    mst.Add(e);
                    total += e.Weight;
                    if (mst.Count == vertexCount - 1) break;
                }
            }

            if (mst.Count != vertexCount - 1)
                throw new InvalidOperationException("The input graph is not connected; MST cannot be formed.");

            return (mst, total);
        }
    }
}
