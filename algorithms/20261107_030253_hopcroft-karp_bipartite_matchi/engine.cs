using System;
using System.Collections.Generic;

namespace BipartiteMatching
{
    public static class HopcroftKarp
    {
        public static HopcroftKarpResult Compute(BipartiteGraph graph)
        {
            int n = graph.LeftSize;
            int m = graph.RightSize;
            int[] pairU = new int[n];
            int[] pairV = new int[m];
            int[] dist = new int[n];

            for (int i = 0; i < n; i++) pairU[i] = -1;
            for (int j = 0; j < m; j++) pairV[j] = -1;

            int matching = 0;
            while (Bfs(graph, pairU, pairV, dist))
            {
                for (int u = 0; u < n; u++)
                {
                    if (pairU[u] == -1)
                    {
                        if (Dfs(u, graph, pairU, pairV, dist))
                            matching++;
                    }
                }
            }

            return new HopcroftKarpResult(matching, pairU, pairV);
        }

        private static bool Bfs(BipartiteGraph graph, int[] pairU, int[] pairV, int[] dist)
        {
            var queue = new Queue<int>();
            for (int u = 0; u < graph.LeftSize; u++)
            {
                if (pairU[u] == -1)
                {
                    dist[u] = 0;
                    queue.Enqueue(u);
                }
                else
                {
                    dist[u] = int.MaxValue;
                }
            }

            bool foundFree = false;
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in graph.Adj[u])
                {
                    int pu = pairV[v];
                    if (pu != -1)
                    {
                        if (dist[pu] == int.MaxValue)
                        {
                            dist[pu] = dist[u] + 1;
                            queue.Enqueue(pu);
                        }
                    }
                    else
                    {
                        foundFree = true;
                    }
                }
            }
            return foundFree;
        }

        private static bool Dfs(int u, BipartiteGraph graph, int[] pairU, int[] pairV, int[] dist)
        {
            foreach (int v in graph.Adj[u])
            {
                int pu = pairV[v];
                if (pu == -1 || (dist[pu] == dist[u] + 1 && Dfs(pu, graph, pairU, pairV, dist)))
                {
                    pairU[u] = v;
                    pairV[v] = u;
                    return true;
                }
            }
            dist[u] = int.MaxValue;
            return false;
        }
    }
}
