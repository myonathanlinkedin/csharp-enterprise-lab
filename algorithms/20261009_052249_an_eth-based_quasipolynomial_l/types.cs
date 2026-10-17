using System;
using System.Collections.Generic;
using System.Linq;

namespace Dualization
{
    /// <summary>
    /// Represents a hypergraph (a family of subsets of a finite ground set).
    /// Each edge is a set of integers representing vertices.
    /// </summary>
    public sealed class Hypergraph
    {
        private readonly List<HashSet<int>> _edges = new List<HashSet<int>>();

        /// <summary>
        /// Gets a read‑only view of the edges.
        /// </summary>
        public IReadOnlyList<HashSet<int>> Edges => _edges;

        /// <summary>
        /// Adds an edge to the hypergraph. The edge is copied to protect internal state.
        /// </summary>
        /// <param name="edge">The vertices of the edge.</param>
        public void AddEdge(IEnumerable<int> edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            var set = new HashSet<int>(edge);
            _edges.Add(set);
        }

        /// <summary>
        /// Returns a deep copy of the hypergraph.
        /// </summary>
        public Hypergraph Clone()
        {
            var copy = new Hypergraph();
            foreach (var e in _edges)
                copy._edges.Add(new HashSet<int>(e));
            return copy;
        }

        /// <summary>
        /// Removes all edges that are supersets of another edge (i.e., keeps only inclusion‑minimal edges).
        /// This operation is useful for preprocessing.
        /// </summary>
        public void ReduceToMinimalEdges()
        {
            var minimal = new List<HashSet<int>>();
            foreach (var e in _edges)
            {
                bool isSuperset = minimal.Any(m => m.IsSubsetOf(e));
                if (!isSuperset)
                {
                    // Remove any existing edge that is a superset of the new one
                    minimal.RemoveAll(m => e.IsSubsetOf(m));
                    minimal.Add(e);
                }
            }
            _edges.Clear();
            _edges.AddRange(minimal);
        }
    }

    /// <summary>
    /// Utility functions for set operations used throughout the engine.
    /// </summary>
    internal static class SetUtils
    {
        /// <summary>
        /// Returns true if <paramref name="a"/> is a proper subset of <paramref name="b"/>.
        /// </summary>
        public static bool IsProperSubsetOf(HashSet<int> a, HashSet<int> b)
        {
            return a.Count < b.Count && a.IsSubsetOf(b);
        }

        /// <summary>
        /// Returns a new set that is the union of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static HashSet<int> Union(HashSet<int> a, HashSet<int> b)
        {
            var result = new HashSet<int>(a);
            result.UnionWith(b);
            return result;
        }

        /// <summary>
        /// Returns a new set that is the difference a \ b.
        /// </summary>
        public static HashSet<int> Difference(HashSet<int> a, HashSet<int> b)
        {
            var result = new HashSet<int>(a);
            result.ExceptWith(b);
            return result;
        }

        /// <summary>
        /// Prunes a collection of sets, removing any set that is a superset of another.
        /// The returned list is minimal with respect to set inclusion.
        /// </summary>
        public static List<HashSet<int>> PruneSupersets(IEnumerable<HashSet<int>> candidates)
        {
            var result = new List<HashSet<int>>();
            foreach (var cand in candidates)
            {
                bool isSuperset = result.Any(r => r.IsSubsetOf(cand));
                if (isSuperset)
                    continue;

                // Remove any existing set that is a superset of the new candidate
                result.RemoveAll(r => cand.IsSubsetOf(r));
                result.Add(cand);
            }
            return result;
        }
    }
}
