using System;
using System.Collections.Generic;

namespace GraphAlgorithms
{
    /// <summary>
    /// Represents a directed graph with generic vertex type.
    /// Provides topological sorting with cycle detection.
    /// </summary>
    /// <typeparam name="T">Vertex type (must be non‑null and hashable).</typeparam>
    public sealed class DirectedGraph<T> where T : notnull
    {
        private readonly Dictionary<T, List<T>> _adjacency = new();

        /// <summary>
        /// Adds a vertex to the graph. No effect if the vertex already exists.
        /// </summary>
        public void AddVertex(T vertex)
        {
            if (!_adjacency.ContainsKey(vertex))
                _adjacency[vertex] = new List<T>();
        }

        /// <summary>
        /// Adds a directed edge from <paramref name="source"/> to <paramref name="target"/>.
        /// Vertices are created automatically if missing.
        /// </summary>
        public void AddEdge(T source, T target)
        {
            AddVertex(source);
            AddVertex(target);
            _adjacency[source].Add(target);
        }

        /// <summary>
        /// Returns a topologically sorted list of vertices.
        /// Throws <see cref="InvalidOperationException"/> if the graph contains a cycle.
        /// </summary>
        public IReadOnlyList<T> TopologicalSort()
        {
            var visited = new HashSet<T>();
            var recursionStack = new HashSet<T>();
            var result = new Stack<T>();

            foreach (var vertex in _adjacency.Keys)
            {
                if (!visited.Contains(vertex))
                {
                    DFS(vertex, visited, recursionStack, result);
                }
            }

            // The stack yields reverse post‑order, which is a valid topological order.
            return new List<T>(result);
        }

        /// <summary>
        /// Detects whether the graph contains a cycle.
        /// If a cycle exists, <paramref name="cycle"/> contains one possible cycle path.
        /// </summary>
        public bool TryGetCycle(out List<T> cycle)
        {
            var visited = new HashSet<T>();
            var path = new Stack<T>();
            var onPath = new HashSet<T>();
            cycle = new List<T>();

            foreach (var vertex in _adjacency.Keys)
            {
                if (!visited.Contains(vertex) && DFSDetect(vertex, visited, onPath, path, ref cycle))
                {
                    cycle.Reverse(); // make it start from the first repeated vertex
                    return true;
                }
            }
            return false;
        }

        private void DFS(T node, HashSet<T> visited, HashSet<T> recursionStack, Stack<T> result)
        {
            visited.Add(node);
            recursionStack.Add(node);

            foreach (var neighbor in _adjacency[node])
            {
                if (!visited.Contains(neighbor))
                {
                    DFS(neighbor, visited, recursionStack, result);
                }
                else if (recursionStack.Contains(neighbor))
                {
                    // Cycle detected.
                    throw new InvalidOperationException("Graph contains a cycle; topological sort not possible.");
                }
            }

            recursionStack.Remove(node);
            result.Push(node);
        }

        private bool DFSDetect(T node, HashSet<T> visited, HashSet<T> onPath, Stack<T> path, ref List<T> cycle)
        {
            visited.Add(node);
            onPath.Add(node);
            path.Push(node);

            foreach (var neighbor in _adjacency[node])
            {
                if (!visited.Contains(neighbor))
                {
                    if (DFSDetect(neighbor, visited, onPath, path, ref cycle))
                        return true;
                }
                else if (onPath.Contains(neighbor))
                {
                    // Build cycle list.
                    var temp = new List<T>();
                    foreach (var v in path)
                    {
                        temp.Add(v);
                        if (EqualityComparer<T>.Default.Equals(v, neighbor))
                            break;
                    }
                    cycle = temp;
                    return true;
                }
            }

            path.Pop();
            onPath.Remove(node);
            return false;
        }
    }
}
