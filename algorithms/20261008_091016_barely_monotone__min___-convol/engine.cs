using System;
using System.Collections.Generic;

namespace BarelyMonotone
{
    public class BarelyMonotoneConvolutionEngine : IConvolutionEngine
    {
        public int[] Convolve(Sequence a, Sequence b)
        {
            int n = a.Length;
            int m = b.Length;
            if (n == 0 || m == 0) return Array.Empty<int>();

            int[] result = new int[n + m - 1];
            for (int i = 0; i < result.Length; i++) result[i] = int.MaxValue;

            var heap = new MinHeap();
            for (int i = 0; i < n; i++)
            {
                heap.Push(new HeapNode(a.Values[i] + b.Values[0], i, 0));
            }

            while (!heap.IsEmpty)
            {
                var node = heap.Pop();
                int k = node.IndexI + node.IndexJ;
                if (node.Sum < result[k]) result[k] = node.Sum;
                if (node.IndexJ + 1 < m)
                {
                    int newSum = a.Values[node.IndexI] + b.Values[node.IndexJ + 1];
                    heap.Push(new HeapNode(newSum, node.IndexI, node.IndexJ + 1));
                }
            }

            return result;
        }

        private class HeapNode : IComparable<HeapNode>
        {
            public readonly int Sum;
            public readonly int IndexI;
            public readonly int IndexJ;
            public HeapNode(int sum, int i, int j) { Sum = sum; IndexI = i; IndexJ = j; }
            public int CompareTo(HeapNode other) => Sum.CompareTo(other.Sum);
        }

        private class MinHeap
        {
            private readonly List<HeapNode> _data = new List<HeapNode>();
            public bool IsEmpty => _data.Count == 0;
            public void Push(HeapNode node)
            {
                _data.Add(node);
                int i = _data.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_data[parent].CompareTo(_data[i]) <= 0) break;
                    var temp = _data[parent];
                    _data[parent] = _data[i];
                    _data[i] = temp;
                    i = parent;
                }
            }
            public HeapNode Pop()
            {
                if (_data.Count == 0) throw new InvalidOperationException();
                var root = _data[0];
                var last = _data[_data.Count - 1];
                _data.RemoveAt(_data.Count - 1);
                if (_data.Count > 0)
                {
                    _data[0] = last;
                    int i = 0;
                    while (true)
                    {
                        int left = 2 * i + 1;
                        int right = 2 * i + 2;
                        int smallest = i;
                        if (left < _data.Count && _data[left].CompareTo(_data[smallest]) < 0) smallest = left;
                        if (right < _data.Count && _data[right].CompareTo(_data[smallest]) < 0) smallest = right;
                        if (smallest == i) break;
                        var temp = _data[i];
                        _data[i] = _data[smallest];
                        _data[smallest] = temp;
                        i = smallest;
                    }
                }
                return root;
            }
        }
    }
}
