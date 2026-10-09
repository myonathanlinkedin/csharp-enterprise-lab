using System;
using System.Collections.Generic;
using System.Linq;

namespace Dualization
{
    /// <summary>
    /// Core algorithmic engine that computes the minimal transversals (dual) of a monotone hypergraph.
    /// The implementation follows the classic recursive decomposition (Berge‑type) algorithm.
    /// </summary>
    public static class Dualizer
    {
        /// <summary>
        /// Computes the dual (the family of all inclusion‑minimal hitting sets) of the given hypergraph.
        /// </summary>
        /// <param name="hypergraph">The input hypergraph (must not be null).</param>
        /// <returns>A list of minimal transversals, each represented as a HashSet of vertices.</returns>
        public static List<HashSet<int>> ComputeDual(Hypergraph hypergraph)
        {
            if (hypergraph == null) throw new ArgumentNullException(nameof(hypergraph));

            // Preprocess: remove redundant edges
            var hg = hypergraph.Clone();
            hg.ReduceToMinimalEdges();

            return MinimalTransversals(hg);
        }

        /// <summary>
        /// Recursive routine that returns all minimal transversals of a hypergraph.
        /// </summary>
        private static List<HashSet<int>> MinimalTransversals(Hypergraph hg)
        {
            // Base cases
            if (hg.Edges.Count == 0)
            {
                // The empty set hits all (zero) edges.
                return new List<HashSet<int>> { new HashSet<int>() };
            }

            if (hg.Edges.Any(e => e.Count == 0))
            {
                // An empty edge cannot be hit; no transversal exists.
                return new List<HashSet<int>>();
            }

            // Choose a vertex v from the first edge (deterministic choice)
            int v = hg.Edges[0].First();

            // H1: edges that do NOT contain v (unchanged)
            var h1 = new Hypergraph();
            foreach (var e in hg.Edges)
                if (!e.Contains(v))
                    h1.AddEdge(e);

            // H2: edges that contain v, but with v removed
            var h2 = new Hypergraph();
            foreach (var e in hg.Edges)
                if (e.Contains(v))
                {
                    var reduced = SetUtils.Difference(e, new HashSet<int> { v });
                    h2.AddEdge(reduced);
                }

            // Recursively compute transversals
            var t1 = MinimalTransversals(h1);
            var t2 = MinimalTransversals(h2);

            // Add v to each transversal from the second branch
            var t2WithV = t2.Select(t => {
                var copy = new HashSet<int>(t);
                copy.Add(v);
                return copy;
            });

            // Union of both families, then prune supersets
            var combined = t1.Concat(t2WithV);
            var pruned = SetUtils.PruneSupersets(combined);
            return pruned;
        }
    }
}
