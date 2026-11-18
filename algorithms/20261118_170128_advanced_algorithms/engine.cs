using System;

namespace AdvancedAlgorithms
{
    /// <summary>
    /// A segment tree with lazy propagation supporting range addition and range sum queries.
    /// Operates on 64‑bit signed integers.
    /// </summary>
    public sealed class SegmentTree : IRangeQueryUpdate<long>
    {
        private readonly int _size;          // original array length
        private readonly long[] _tree;       // segment tree nodes (sum)
        private readonly long[] _lazy;       // pending lazy additions

        /// <summary>
        /// Constructs a segment tree from the supplied source array.
        /// </summary>
        /// <param name="source">Array of values to build the tree on. Must not be null.</param>
        public SegmentTree(long[] source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _size = source.Length;
            if (_size == 0) throw new ArgumentException("Source array must contain at least one element.", nameof(source));

            // Allocate 4 * n to guarantee enough space for a binary segment tree.
            _tree = new long[4 * _size];
            _lazy = new long[4 * _size];
            Build(1, 0, _size - 1, source);
        }

        private void Build(int node, int left, int right, long[] source)
        {
            if (left == right)
            {
                _tree[node] = source[left];
                return;
            }

            int mid = left + (right - left) / 2;
            Build(node * 2, left, mid, source);
            Build(node * 2 + 1, mid + 1, right, source);
            _tree[node] = _tree[node * 2] + _tree[node * 2 + 1];
        }

        /// <inheritdoc/>
        public void UpdateRange(int left, int right, long delta)
        {
            ValidateRange(left, right);
            UpdateRange(1, 0, _size - 1, left, right, delta);
        }

        private void UpdateRange(int node, int nodeLeft, int nodeRight, int queryLeft, int queryRight, long delta)
        {
            // Apply any pending lazy updates first.
            if (_lazy[node] != 0)
            {
                ApplyLazy(node, nodeLeft, nodeRight);
            }

            // No overlap.
            if (queryRight < nodeLeft || queryLeft > nodeRight)
                return;

            // Total overlap.
            if (queryLeft <= nodeLeft && nodeRight <= queryRight)
            {
                _lazy[node] += delta;
                ApplyLazy(node, nodeLeft, nodeRight);
                return;
            }

            // Partial overlap.
            int mid = nodeLeft + (nodeRight - nodeLeft) / 2;
            UpdateRange(node * 2, nodeLeft, mid, queryLeft, queryRight, delta);
            UpdateRange(node * 2 + 1, mid + 1, nodeRight, queryLeft, queryRight, delta);
            _tree[node] = _tree[node * 2] + _tree[node * 2 + 1];
        }

        /// <inheritdoc/>
        public long QueryRange(int left, int right)
        {
            ValidateRange(left, right);
            return QueryRange(1, 0, _size - 1, left, right);
        }

        private long QueryRange(int node, int nodeLeft, int nodeRight, int queryLeft, int queryRight)
        {
            if (_lazy[node] != 0)
            {
                ApplyLazy(node, nodeLeft, nodeRight);
            }

            // No overlap.
            if (queryRight < nodeLeft || queryLeft > nodeRight)
                return 0L;

            // Total overlap.
            if (queryLeft <= nodeLeft && nodeRight <= queryRight)
                return _tree[node];

            // Partial overlap.
            int mid = nodeLeft + (nodeRight - nodeLeft) / 2;
            long leftSum = QueryRange(node * 2, nodeLeft, mid, queryLeft, queryRight);
            long rightSum = QueryRange(node * 2 + 1, mid + 1, nodeRight, queryLeft, queryRight);
            return leftSum + rightSum;
        }

        private void ApplyLazy(int node, int left, int right)
        {
            // Apply pending addition to this node.
            _tree[node] += _lazy[node] * (right - left + 1);
            if (left != right) // propagate to children if not a leaf.
            {
                _lazy[node * 2] += _lazy[node];
                _lazy[node * 2 + 1] += _lazy[node];
            }
            _lazy[node] = 0;
        }

        private void ValidateRange(int left, int right)
        {
            if (left < 0 || right < 0 || left >= _size || right >= _size)
                throw new ArgumentOutOfRangeException($"Query indices must be within [0, {_size - 1}].");
            if (left > right)
                throw new ArgumentException("Left index cannot be greater than right index.");
        }
    }
}
