using System;
using System.Collections.Generic;

namespace SSSPExample
{
    public class Graph
    {
        public int VertexCount { get; }
        private readonly List<(int to, int weight)>[] _adj;

        public Graph(int vertexCount)
        {
            if (vertexCount <= 0) throw new ArgumentException("Vertex count must be positive.");
            VertexCount = vertexCount;
            _adj = new List<(int to, int weight)>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                _adj[i] = new List<(int to, int weight)>();
        }

        public void AddEdge(int from, int to, int weight)
        {
            if (from < 0 || from >= VertexCount) throw new ArgumentOutOfRangeException(nameof(from));
            if (to < 0 || to >= VertexCount) throw new ArgumentOutOfRangeException(nameof(to));
            if (weight < 0) throw new ArgumentException("Edge weight must be non-negative.", nameof(weight));
            _adj[from].Add((to, weight));
        }

        public IEnumerable<(int to, int weight)> GetNeighbors(int vertex)
        {
            if (vertex < 0 || vertex >= VertexCount) throw new ArgumentOutOfRangeException(nameof(vertex));
            return _adj[vertex];
        }
    }

    public static class SSSP
    {
        // Linear SSSP for DAGs: O(V + E)
        public static int[] ShortestPathDAG(Graph g, int source)
        {
            if (source < 0 || source >= g.VertexCount) throw new ArgumentOutOfRangeException(nameof(source));

            // Kahn's algorithm for topological sort
            int[] indegree = new int[g.VertexCount];
            for (int v = 0; v < g.VertexCount; v++)
                foreach (var (_, w) in g.GetNeighbors(v))
                    indegree[w]++;

            var queue = new Queue<int>();
            for (int v = 0; v < g.VertexCount; v++)
                if (indegree[v] == 0)
                    queue.Enqueue(v);

            var topo = new List<int>();
            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                topo.Add(v);
                foreach (var (to, _) in g.GetNeighbors(v))
                {
                    if (--indegree[to] == 0)
                        queue.Enqueue(to);
                }
            }

            if (topo.Count != g.VertexCount)
                throw new InvalidOperationException("Graph is not a DAG.");

            int[] dist = new int[g.VertexCount];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            dist[source] = 0;

            foreach (int v in topo)
            {
                if (dist[v] == int.MaxValue) continue;
                foreach (var (to, w) in g.GetNeighbors(v))
                {
                    if (dist[v] + w < dist[to])
                        dist[to] = dist[v] + w;
                }
            }

            return dist;
        }

        // Dial's algorithm for graphs with non-negative integer weights: O(V + E + maxWeight * V)
        public static int[] ShortestPathDial(Graph g, int source, int maxWeight)
        {
            if (source < 0 || source >= g.VertexCount) throw new ArgumentOutOfRangeException(nameof(source));
            if (maxWeight < 0) throw new ArgumentException("maxWeight must be non-negative.", nameof(maxWeight));

            int V = g.VertexCount;
            int maxDistance = maxWeight * (V - 1);
            var buckets = new List<int>[maxDistance + 1];
            for (int i = 0; i <= maxDistance; i++)
                buckets[i] = new List<int>();

            int[] dist = new int[V];
            for (int i = 0; i < V; i++) dist[i] = int.MaxValue;
            dist[source] = 0;
            buckets[0].Add(source);

            for (int d = 0; d <= maxDistance; d++)
            {
                var bucket = buckets[d];
                int idx = 0;
                while (idx < bucket.Count)
                {
                    int v = bucket[idx++];
                    if (dist[v] != d) continue; // stale entry
                    foreach (var (to, w) in g.GetNeighbors(v))
                    {
                        int nd = d + w;
                        if (nd > maxDistance) continue; // ignore distances beyond maxDistance
                        if (nd < dist[to])
                        {
                            dist[to] = nd;
                            buckets[nd].Add(to);
                        }
                    }
                }
            }

            return dist;
        }
    }
}
