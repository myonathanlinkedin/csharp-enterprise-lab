using System;
using System.Collections.Generic;

namespace BipartiteMatching
{
    public class BipartiteGraph
    {
        public int LeftSize { get; }
        public int RightSize { get; }
        public List<int>[] Adj { get; }

        public BipartiteGraph(int leftSize, int rightSize)
        {
            if (leftSize < 0) throw new ArgumentOutOfRangeException(nameof(leftSize));
            if (rightSize < 0) throw new ArgumentOutOfRangeException(nameof(rightSize));

            LeftSize = leftSize;
            RightSize = rightSize;
            Adj = new List<int>[leftSize];
            for (int i = 0; i < leftSize; i++)
                Adj[i] = new List<int>();
        }

        public void AddEdge(int left, int right)
        {
            if (left < 0 || left >= LeftSize) throw new ArgumentOutOfRangeException(nameof(left));
            if (right < 0 || right >= RightSize) throw new ArgumentOutOfRangeException(nameof(right));
            Adj[left].Add(right);
        }
    }

    public class HopcroftKarpResult
    {
        public int MatchingSize { get; }
        public int[] PairU { get; } // left -> right
        public int[] PairV { get; } // right -> left

        public HopcroftKarpResult(int matchingSize, int[] pairU, int[] pairV)
        {
            MatchingSize = matchingSize;
            PairU = pairU;
            PairV = pairV;
        }
    }
}
