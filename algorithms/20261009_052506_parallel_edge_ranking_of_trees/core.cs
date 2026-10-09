using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EdgeRanking
{
    // Simple immutable edge representation
    public sealed class Edge
    {
        public int Id { get; }
        public int NodeA { get; }
        public int NodeB { get; }

        public Edge(int id, int nodeA, int nodeB)
        {
            Id = id;
            NodeA = nodeA;
            NodeB = nodeB;
        }

        // Returns the opposite vertex of the supplied one
        public int Opposite(int vertex) => vertex == NodeA ? NodeB : NodeA;
    }

    // Tree structure (undirected, connected, acyclic)
    public sealed class Tree
    {
        public IReadOnlyList<int> Nodes { get; }
        public IReadOnlyList<Edge> Edges { get; }

        // adjacency: node -> list of incident edges
        private readonly Dictionary<int, List<Edge>> _adjacency;

        public Tree(IEnumerable<int> nodes, IEnumerable<Edge> edges)
        {
            Nodes = new List<int>(nodes);
            Edges = new List<Edge>(edges);
            _adjacency = new Dictionary<int, List<Edge>>();
            foreach (var n in Nodes)
                _adjacency[n] = new List<Edge>();

            foreach (var e in Edges)
            {
                if (!_adjacency.ContainsKey(e.NodeA) || !_adjacency.ContainsKey(e.NodeB))
                    throw new ArgumentException("Edge references undefined node.");
                _adjacency[e.NodeA].Add(e);
                _adjacency[e.NodeB].Add(e);
            }
        }

        public IReadOnlyList<Edge> IncidentEdges(int node) => _adjacency[node];
    }

    // Core algorithm: compute a valid edge ranking for any tree.
    public static class EdgeRankingAlgorithm
    {
        // Returns a mapping EdgeId -> Rank (positive integers, 1 = lowest)
        public static IDictionary<int, int> ComputeEdgeRanking(Tree tree)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            if (tree.Nodes.Count == 0) return new Dictionary<int, int>();

            // Choose arbitrary root (first node)
            int root = tree.Nodes[0];
            var rankMap = new ConcurrentDictionary<int, int>();
            var visited = new HashSet<int>();
            visited.Add(root);

            // Recursive DFS that returns height of subtree rooted at 'node'
            int Dfs(int node, int parentEdgeId)
            {
                var childEdges = tree.IncidentEdges(node);
                var childHeights = new List<int>();

                // Process children in parallel
                Parallel.ForEach(childEdges, edge =>
                {
                    int neighbor = edge.Opposite(node);
                    // Avoid revisiting parent
                    lock (visited)
                    {
                        if (visited.Contains(neighbor)) return;
                        visited.Add(neighbor);
                    }

                    int childHeight = Dfs(neighbor, edge.Id);
                    // Record rank for the edge connecting node to neighbor
                    // Rank equals child's subtree height (>=1)
                    rankMap[edge.Id] = childHeight;
                    lock (childHeights) { childHeights.Add(childHeight); }
                });

                // Height of current node = 0 if leaf, else 1 + max child height
                if (childHeights.Count == 0) return 1; // leaf contributes height 1 to its incident edge
                int maxChild = 0;
                foreach (var h in childHeights) if (h > maxChild) maxChild = h;
                return maxChild + 1;
            }

            // Start DFS; root has no parent edge (use -1 placeholder)
            Dfs(root, -1);
            return rankMap;
        }

        // Verification routine: returns true iff the ranking satisfies the edge‑ranking property.
        public static bool VerifyRanking(Tree tree, IDictionary<int, int> ranking)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            if (ranking == null) throw new ArgumentNullException(nameof(ranking));
            // Build edge lookup by id
            var edgeById = new Dictionary<int, Edge>();
            foreach (var e in tree.Edges) edgeById[e.Id] = e;

            // Group edges by rank
            var groups = new Dictionary<int, List<int>>();
            foreach (var kvp in ranking)
            {
                if (!groups.TryGetValue(kvp.Value, out var list))
                {
                    list = new List<int>();
                    groups[kvp.Value] = list;
                }
                list.Add(kvp.Key);
            }

            // For each rank group, check the condition
            foreach (var kvp in groups)
            {
                int rank = kvp.Key;
                var edgesInGroup = kvp.Value;
                // Compare every unordered pair
                for (int i = 0; i < edgesInGroup.Count; i++)
                {
                    for (int j = i + 1; j < edgesInGroup.Count; j++)
                    {
                        var e1 = edgeById[edgesInGroup[i]];
                        var e2 = edgeById[edgesInGroup[j]];
                        // Find node path between the two edges (via their incident nodes)
                        var pathEdges = FindEdgePath(tree, e1, e2);
                        bool higherFound = false;
                        foreach (var pe in pathEdges)
                        {
                            if (ranking.TryGetValue(pe.Id, out int r) && r > rank)
                            {
                                higherFound = true;
                                break;
                            }
                        }
                        if (!higherFound) return false;
                    }
                }
            }
            return true;
        }

        // Helper: returns list of edges on the unique simple path between two edges.
        private static List<Edge> FindEdgePath(Tree tree, Edge eStart, Edge eEnd)
        {
            // Choose arbitrary endpoint for each edge as start nodes
            int startNode = eStart.NodeA;
            int targetNode = eEnd.NodeA;

            // BFS to find node path
            var prev = new Dictionary<int, (int parentNode, Edge viaEdge)>();
            var queue = new Queue<int>();
            queue.Enqueue(startNode);
            prev[startNode] = (-1, null);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                if (cur == targetNode) break;
                foreach (var edge in tree.IncidentEdges(cur))
                {
                    int nb = edge.Opposite(cur);
                    if (prev.ContainsKey(nb)) continue;
                    prev[nb] = (cur, edge);
                    queue.Enqueue(nb);
                }
            }

            // Reconstruct node path
            var nodePath = new List<int>();
            var edgePath = new List<Edge>();
            int walk = targetNode;
            while (walk != -1 && prev.ContainsKey(walk))
            {
                nodePath.Add(walk);
                var (parent, via) = prev[walk];
                if (via != null) edgePath.Add(via);
                walk = parent;
            }
            edgePath.Reverse(); // now from startNode towards targetNode

            // The path between edges includes eStart and eEnd themselves
            var fullPath = new List<Edge> { eStart };
            fullPath.AddRange(edgePath);
            fullPath.Add(eEnd);
            return fullPath;
        }
    }
}
