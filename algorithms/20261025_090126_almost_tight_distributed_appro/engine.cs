using System.Collections.Generic;
using System.Linq;

namespace DominationApprox
{
    /// <summary>
    /// Provides a deterministic greedy approximation algorithm for the Minimum Dominating Set problem.
    /// The algorithm repeatedly selects a vertex of maximum uncovered degree, adds it to the dominating set,
    /// and marks it and all its neighbors as dominated.
    /// This yields an O(log Δ) approximation where Δ is the maximum degree.
    /// </summary>
    public static class DominationApproximator
    {
        /// <summary>
        /// Computes a dominating set for the supplied graph.
        /// </summary>
        /// <param name="g">The input graph (must be non‑null).</param>
        /// <returns>A set of vertex identifiers that dominate the graph.</returns>
        public static HashSet<int> ComputeDominatingSet(Graph g)
        {
            if (g == null) throw new System.ArgumentNullException(nameof(g));

            // Result set
            var dominatingSet = new HashSet<int>();

            // Tracks which vertices are already dominated
            var dominated = new HashSet<int>();

            // Helper to mark a vertex and its neighbors as dominated
            void MarkDominated(int v)
            {
                if (!dominated.Add(v)) return; // already dominated
                foreach (var nb in g.Neighbors(v))
                {
                    dominated.Add(nb);
                }
            }

            // Process isolated vertices first (they must belong to any dominating set)
            foreach (var v in g.Vertices)
            {
                if (g.Degree(v) == 0)
                {
                    dominatingSet.Add(v);
                    dominated.Add(v);
                }
            }

            // Main greedy loop
            while (dominated.Count < g.Vertices.Count())
            {
                // Choose an undominated vertex with the highest number of undominated neighbors
                int bestVertex = -1;
                int bestScore = -1;

                foreach (var v in g.Vertices)
                {
                    if (dominated.Contains(v)) continue;

                    int score = 0;
                    foreach (var nb in g.Neighbors(v))
                    {
                        if (!dominated.Contains(nb))
                            score++;
                    }

                    // Include the vertex itself in the score (it will dominate itself)
                    score++; // self‑domination

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestVertex = v;
                    }
                }

                // Defensive check (should never happen)
                if (bestVertex == -1) throw new System.InvalidOperationException("Failed to find a vertex to dominate remaining nodes.");

                dominatingSet.Add(bestVertex);
                MarkDominated(bestVertex);
            }

            return dominatingSet;
        }
    }
}
