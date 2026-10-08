using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumCompression
{
    public class QuantumRetriever
    {
        private readonly int[] _data;
        private readonly int _n;
        private readonly int _k;
        private readonly int[,] _dp;
        private readonly int[,] _cost;
        private readonly int[,] _choice;

        public QuantumRetriever(int[] data, int k)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (k < 1 || k > data.Length) throw new ArgumentOutOfRangeException(nameof(k));

            _data = data;
            _n = data.Length;
            _k = k;

            _cost = new int[_n, _n];
            _dp = new int[_k + 1, _n];
            _choice = new int[_k + 1, _n];

            PrecomputeCosts();
            ComputeDP();
        }

        private void PrecomputeCosts()
        {
            for (int i = 0; i < _n; i++)
            {
                _cost[i, i] = 0;
                for (int j = i + 1; j < _n; j++)
                {
                    int min = _data[i];
                    int max = _data[i];
                    for (int m = i + 1; m <= j; m++)
                    {
                        if (_data[m] < min) min = _data[m];
                        if (_data[m] > max) max = _data[m];
                    }
                    _cost[i, j] = max - min;
                }
            }
        }

        private void ComputeDP()
        {
            for (int i = 0; i < _n; i++)
            {
                _dp[1, i] = _cost[0, i];
                _choice[1, i] = 0;
            }

            for (int k = 2; k <= _k; k++)
            {
                for (int i = k - 1; i < _n; i++)
                {
                    int best = int.MaxValue;
                    int bestSplit = -1;
                    for (int s = k - 2; s < i; s++)
                    {
                        int val = _dp[k - 1, s] + _cost[s + 1, i];
                        if (val < best)
                        {
                            best = val;
                            bestSplit = s;
                        }
                    }
                    _dp[k, i] = best;
                    _choice[k, i] = bestSplit;
                }
            }
        }

        public int GetOptimalCost()
        {
            return _dp[_k, _n - 1];
        }

        public int[] GetOptimalPartitions()
        {
            var boundaries = new List<int>();
            int k = _k;
            int i = _n - 1;

            while (k > 1)
            {
                int split = _choice[k, i];
                boundaries.Add(split + 1);
                i = split;
                k--;
            }

            boundaries.Add(0);
            boundaries.Reverse();
            return boundaries.ToArray();
        }

        public int[] GetCompressedRepresentation()
        {
            var partitions = GetOptimalPartitions();
            var result = new List<int>();
            int prevEnd = 0;

            for (int p = 0; p < partitions.Length; p++)
            {
                int start = p == 0 ? 0 : partitions[p - 1] + 1;
                int end = partitions[p];
                int min = _data[start];
                int max = _data[start];
                for (int m = start + 1; m <= end; m++)
                {
                    if (_data[m] < min) min = _data[m];
                    if (_data[m] > max) max = _data[m];
                }
                result.Add(min);
                result.Add(max);
            }

            return result.ToArray();
        }
    }
}
