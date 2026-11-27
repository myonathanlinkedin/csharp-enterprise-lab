using System.Collections.Generic;

namespace LocalCountingPathDetection
{
    /// <summary>
    /// Implements a local‑counting (depth‑first) algorithm to detect an induced path
    /// of a given length in an undirected graph.
    /// </summary>
    public static class PathDetector
    {
        /// <summary>
        /// Determines whether the graph <paramref name="g"/> contains an induced path
        /// consisting of exactly <paramref name="length"/> edges (i.e., length+1 vertices).
        /// </summary>
        /// <param name="g">The graph to search.</param>
        /// <param name="length">Number of edges in the desired induced path. Must be &gt;= 0.</param>
        /// <returns>True if such a path exists; otherwise false.</returns>
        public static bool HasInducedPath(Graph g, int length)
        {
            if (length < 0) throw new System.ArgumentOutOfRangeException(nameof(length));
            // Length 0 corresponds to a single vertex.
            if (length == 0) return AnyVertexExists(g);

            foreach (int start in g.Vertices)
            {
                var visited = new HashSet<int> { start };
                if (SearchFrom(g, start, -1, length, visited))
                    return true;
            }
            return false;
        }

        private static bool AnyVertexExists(Graph g) => System.Linq.Enumerable.Any(g.Vertices);

        /// <summary>
        /// Recursive depth‑first search that attempts to extend an induced path.
        /// </summary>
        /// <param name="g">Graph.</param>
        /// <param name="current">Current endpoint of the partial path.</param>
        /// <param name="previous">Previous vertex on the path (or -1 if none).</param>
        /// <param name="remaining">Number of edges still required to reach the target length.</param>
        /// <param name="visited">Set of vertices already on the path.</param>
        /// <returns>True if a full induced path is found.</returns>
        private static bool SearchFrom(Graph g, int current, int previous, int remaining, HashSet<int> visited)
        {
            if (remaining == 0) return true; // Desired length achieved.

            foreach (int neighbor in g.GetNeighbors(current))
            {
                if (neighbor == previous) continue; // Do not go back directly.
                if (visited.Contains(neighbor)) continue; // Simple path constraint.

                // Induced property: neighbor must not be adjacent to any vertex already in the path
                // except the current endpoint.
                if (HasForbiddenChord(g, neighbor, visited, current))
                    continue;

                visited.Add(neighbor);
                bool found = SearchFrom(g, neighbor, current, remaining - 1, visited);
                visited.Remove(neighbor);
                if (found) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether adding <paramref name="candidate"/> would create a chord
        /// with the existing <paramref name="pathVertices"/> (excluding <paramref name="allowedNeighbor"/>).
        /// </summary>
        private static bool HasForbiddenChord(Graph g, int candidate, HashSet<int> pathVertices, int allowedNeighbor)
        {
            foreach (int v in pathVertices)
            {
                if (v == allowedNeighbor) continue;
                // If candidate is adjacent to any earlier vertex, the path would not be induced.
                if (g.GetNeighbors(candidate).Contains(v))
                    return true;
            }
            return false;
        }
    }
}
