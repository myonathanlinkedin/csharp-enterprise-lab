using System;
using System.Collections.Generic;

namespace ChronosAVL
{
    public sealed class AVLTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private sealed class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public int Height;
            public Node PrevChrono;
            public Node NextChrono;

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
                Height = 1;
            }
        }

        private Node _root;
        private Node _headChrono;
        private Node _tailChrono;
        private int _count;

        public int Count => _count;

        // Public Insert: returns true if inserted, false if key existed (value updated)
        public bool Insert(TKey key, TValue value)
        {
            bool inserted;
            _root = Insert(_root, key, value, out inserted);
            if (inserted) _count++;
            return inserted;
        }

        // Public Delete: returns true if key existed and removed
        public bool Delete(TKey key)
        {
            bool removed;
            _root = Delete(_root, key, out removed);
            if (removed) _count--;
            return removed;
        }

        // TryGetValue
        public bool TryGetValue(TKey key, out TValue value)
        {
            Node cur = _root;
            while (cur != null)
            {
                int cmp = key.CompareTo(cur.Key);
                if (cmp == 0)
                {
                    value = cur.Value;
                    return true;
                }
                cur = cmp < 0 ? cur.Left : cur.Right;
            }
            value = default!;
            return false;
        }

        // In-order traversal (sorted by key)
        public IEnumerable<KeyValuePair<TKey, TValue>> InOrder()
        {
            var stack = new Stack<Node>();
            Node cur = _root;
            while (stack.Count > 0 || cur != null)
            {
                while (cur != null)
                {
                    stack.Push(cur);
                    cur = cur.Left;
                }
                cur = stack.Pop();
                yield return new KeyValuePair<TKey, TValue>(cur.Key, cur.Value);
                cur = cur.Right;
            }
        }

        // Chronological traversal (order of insertion)
        public IEnumerable<KeyValuePair<TKey, TValue>> Chronological()
        {
            for (Node cur = _headChrono; cur != null; cur = cur.NextChrono)
                yield return new KeyValuePair<TKey, TValue>(cur.Key, cur.Value);
        }

        // ---------- Internal AVL Operations ----------
        private Node Insert(Node node, TKey key, TValue value, out bool inserted)
        {
            if (node == null)
            {
                var newNode = new Node(key, value);
                LinkChrono(newNode);
                inserted = true;
                return newNode;
            }

            int cmp = key.CompareTo(node.Key);
            if (cmp < 0)
                node.Left = Insert(node.Left, key, value, out inserted);
            else if (cmp > 0)
                node.Right = Insert(node.Right, key, value, out inserted);
            else
            {
                // Key exists: update value, do not change chronology
                node.Value = value;
                inserted = false;
                return node;
            }

            UpdateHeight(node);
            return Balance(node);
        }

        private Node Delete(Node node, TKey key, out bool removed)
        {
            if (node == null)
            {
                removed = false;
                return null;
            }

            int cmp = key.CompareTo(node.Key);
            if (cmp < 0)
                node.Left = Delete(node.Left, key, out removed);
            else if (cmp > 0)
                node.Right = Delete(node.Right, key, out removed);
            else
            {
                // Node to delete found
                removed = true;
                UnlinkChrono(node);
                if (node.Left == null) return node.Right;
                if (node.Right == null) return node.Left;

                // Replace with inorder successor
                Node succ = MinNode(node.Right);
                node.Key = succ.Key;
                node.Value = succ.Value;
                // Preserve chronology: successor already linked; no changes needed
                node.Right = Delete(node.Right, succ.Key, out bool _);
            }

            UpdateHeight(node);
            return Balance(node);
        }

        private static Node MinNode(Node node)
        {
            while (node.Left != null) node = node.Left;
            return node;
        }

        private static void UpdateHeight(Node node)
        {
            int leftH = node.Left?.Height ?? 0;
            int rightH = node.Right?.Height ?? 0;
            node.Height = Math.Max(leftH, rightH) + 1;
        }

        private static int BalanceFactor(Node node) => (node.Right?.Height ?? 0) - (node.Left?.Height ?? 0);

        private static Node RotateRight(Node y)
        {
            Node x = y.Left;
            Node T2 = x.Right;
            x.Right = y;
            y.Left = T2;
            UpdateHeight(y);
            UpdateHeight(x);
            return x;
        }

        private static Node RotateLeft(Node x)
        {
            Node y = x.Right;
            Node T2 = y.Left;
            y.Left = x;
            x.Right = T2;
            UpdateHeight(x);
            UpdateHeight(y);
            return y;
        }

        private Node Balance(Node node)
        {
            int bf = BalanceFactor(node);
            if (bf < -1)
            {
                // Left heavy
                if (BalanceFactor(node.Left) > 0)
                    node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }
            if (bf > 1)
            {
                // Right heavy
                if (BalanceFactor(node.Right) < 0)
                    node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }
            return node;
        }

        // ---------- Chronological Linking ----------
        private void LinkChrono(Node node)
        {
            if (_headChrono == null)
            {
                _headChrono = _tailChrono = node;
            }
            else
            {
                _tailChrono!.NextChrono = node;
                node.PrevChrono = _tailChrono;
                _tailChrono = node;
            }
        }

        private void UnlinkChrono(Node node)
        {
            if (node.PrevChrono != null)
                node.PrevChrono.NextChrono = node.NextChrono;
            else
                _headChrono = node.NextChrono;

            if (node.NextChrono != null)
                node.NextChrono.PrevChrono = node.PrevChrono;
            else
                _tailChrono = node.PrevChrono;

            node.PrevChrono = node.NextChrono = null;
        }
    }
}
