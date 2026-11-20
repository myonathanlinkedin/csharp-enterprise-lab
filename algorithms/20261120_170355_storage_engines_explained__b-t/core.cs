using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageEngines
{
    // -------------------- B-Tree Implementation --------------------
    public class BTreeNode<TKey, TValue> where TKey : IComparable<TKey>
    {
        public bool IsLeaf { get; private set; }
        public List<TKey> Keys { get; }
        public List<TValue> Values { get; }
        public List<BTreeNode<TKey, TValue>> Children { get; }

        private readonly int _t; // Minimum degree

        public BTreeNode(int t, bool isLeaf)
        {
            if (t < 2) throw new ArgumentException("Minimum degree t must be >= 2");
            _t = t;
            IsLeaf = isLeaf;
            Keys = new List<TKey>();
            Values = new List<TValue>();
            Children = new List<BTreeNode<TKey, TValue>>();
        }

        // Search for a key in the subtree rooted at this node.
        public (bool found, TValue value) Search(TKey key)
        {
            int i = 0;
            while (i < Keys.Count && key.CompareTo(Keys[i]) > 0)
                i++;

            if (i < Keys.Count && key.CompareTo(Keys[i]) == 0)
                return (true, Values[i]);

            if (IsLeaf)
                return (false, default!);

            return Children[i].Search(key);
        }

        // Split child y at index i (y must be full).
        public void SplitChild(int i, BTreeNode<TKey, TValue> y)
        {
            var z = new BTreeNode<TKey, TValue>(_t, y.IsLeaf);
            // Move t-1 keys from y to z
            for (int j = 0; j < _t - 1; j++)
            {
                z.Keys.Add(y.Keys[_t]);
                z.Values.Add(y.Values[_t]);
                y.Keys.RemoveAt(_t);
                y.Values.RemoveAt(_t);
            }

            if (!y.IsLeaf)
            {
                for (int j = 0; j < _t; j++)
                {
                    z.Children.Add(y.Children[_t]);
                    y.Children.RemoveAt(_t);
                }
            }

            // Insert new child into this node
            Children.Insert(i + 1, z);
            // Move middle key up
            Keys.Insert(i, y.Keys[_t - 1]);
            Values.Insert(i, y.Values[_t - 1]);
            y.Keys.RemoveAt(_t - 1);
            y.Values.RemoveAt(_t - 1);
        }

        // Insert non-full
        public void InsertNonFull(TKey key, TValue value)
        {
            int i = Keys.Count - 1;
            if (IsLeaf)
            {
                // Find location to insert
                while (i >= 0 && key.CompareTo(Keys[i]) < 0)
                {
                    i--;
                }
                Keys.Insert(i + 1, key);
                Values.Insert(i + 1, value);
            }
            else
            {
                while (i >= 0 && key.CompareTo(Keys[i]) < 0)
                    i--;
                i++;
                if (Children[i].Keys.Count == 2 * _t - 1)
                {
                    SplitChild(i, Children[i]);
                    if (key.CompareTo(Keys[i]) > 0)
                        i++;
                }
                Children[i].InsertNonFull(key, value);
            }
        }
    }

    public class BTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private readonly int _t;
        private BTreeNode<TKey, TValue> _root;

        public BTree(int minimumDegree)
        {
            if (minimumDegree < 2) throw new ArgumentException("Minimum degree must be >= 2");
            _t = minimumDegree;
            _root = new BTreeNode<TKey, TValue>(_t, true);
        }

        public (bool found, TValue value) Search(TKey key) => _root.Search(key);

        public void Insert(TKey key, TValue value)
        {
            if (_root.Keys.Count == 2 * _t - 1)
            {
                var s = new BTreeNode<TKey, TValue>(_t, false);
                s.Children.Add(_root);
                s.SplitChild(0, _root);
                _root = s;
            }
            _root.InsertNonFull(key, value);
        }
    }

    // -------------------- LSM Tree Implementation --------------------
    // Simplified LSM Tree: in‑memory memtable + immutable sorted runs (simulated in RAM).
    public class LSMTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private readonly int _memtableThreshold;
        private readonly int _runMergeThreshold;
        private SortedDictionary<TKey, TValue> _memtable;
        private readonly List<List<KeyValuePair<TKey, TValue>>> _runs;

        public LSMTree(int memtableThreshold = 4, int runMergeThreshold = 3)
        {
            if (memtableThreshold <= 0) throw new ArgumentException("memtableThreshold must be > 0");
            if (runMergeThreshold <= 1) throw new ArgumentException("runMergeThreshold must be > 1");
            _memtableThreshold = memtableThreshold;
            _runMergeThreshold = runMergeThreshold;
            _memtable = new SortedDictionary<TKey, TValue>();
            _runs = new List<List<KeyValuePair<TKey, TValue>>>();
        }

        // Insert into memtable; flush if needed.
        public void Insert(TKey key, TValue value)
        {
            _memtable[key] = value;
            if (_memtable.Count >= _memtableThreshold)
                FlushMemtable();
        }

        // Search across memtable and runs (most recent first).
        public (bool found, TValue value) Search(TKey key)
        {
            if (_memtable.TryGetValue(key, out var v))
                return (true, v);

            foreach (var run in _runs)
            {
                int idx = run.BinarySearch(new KeyValuePair<TKey, TValue>(key, default!),
                    Comparer<KeyValuePair<TKey, TValue>>.Create((a, b) => a.Key.CompareTo(b.Key)));
                if (idx >= 0)
                    return (true, run[idx].Value);
            }
            return (false, default!);
        }

        // Flush memtable to a new immutable sorted run.
        private void FlushMemtable()
        {
            var newRun = _memtable.Select(kv => new KeyValuePair<TKey, TValue>(kv.Key, kv.Value)).ToList();
            _runs.Insert(0, newRun);
            _memtable = new SortedDictionary<TKey, TValue>();
            if (_runs.Count > _runMergeThreshold)
                MergeRuns();
        }

        // Merge the two oldest runs into one sorted run.
        private void MergeRuns()
        {
            // Simple pairwise merge of the two oldest runs.
            int n = _runs.Count;
            var left = _runs[n - 2];
            var right = _runs[n - 1];
            var merged = new List<KeyValuePair<TKey, TValue>>(left.Count + right.Count);
            int i = 0, j = 0;
            while (i < left.Count && j < right.Count)
            {
                if (left[i].Key.CompareTo(right[j].Key) <= 0)
                {
                    merged.Add(left[i]);
                    i++;
                }
                else
                {
                    merged.Add(right[j]);
                    j++;
                }
            }
            while (i < left.Count) merged.Add(left[i++]);
            while (j < right.Count) merged.Add(right[j++]);

            // Replace the two runs with the merged run.
            _runs.RemoveAt(n - 1);
            _runs.RemoveAt(n - 2);
            _runs.Add(merged);
        }

        // For testing: expose internal state (read‑only).
        public IReadOnlyList<IReadOnlyList<KeyValuePair<TKey, TValue>>> Runs => _runs;
        public IReadOnlyDictionary<TKey, TValue> MemTable => _memtable;
    }
}
