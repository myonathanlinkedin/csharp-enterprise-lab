using System;
using System.Collections.Generic;

namespace OptimisticLockCoupling
{
    /// <summary>
    /// Thread‑safe sorted linked list using safe optimistic lock coupling.
    /// Supports Add, Remove and Contains for items implementing IComparable&lt;T&gt;.
    /// </summary>
    public sealed class OptimisticList<T> where T : IComparable<T>
    {
        private sealed class Node
        {
            public readonly T Value;
            public Node Next;
            public bool Marked;          // logical deletion flag
            public readonly bool Sentinel;
            public readonly object LockObj = new object();

            public Node(T value, Node next, bool sentinel = false)
            {
                Value = value;
                Next = next;
                Sentinel = sentinel;
                Marked = false;
            }
        }

        private readonly Node _head;
        private readonly Node _tail;

        public OptimisticList()
        {
            _tail = new Node(default!, null, sentinel: true);
            _head = new Node(default!, _tail, sentinel: true);
        }

        /// <summary>
        /// Adds an item to the list if it is not already present.
        /// Returns true if the item was inserted, false if it already existed.
        /// </summary>
        public bool Add(T item)
        {
            while (true)
            {
                var (pred, curr) = Find(item);
                lock (pred.LockObj)
                {
                    lock (curr.LockObj)
                    {
                        if (!Validate(pred, curr))
                            continue; // retry

                        if (!curr.Sentinel && curr.Value.CompareTo(item) == 0)
                            return false; // already present

                        var node = new Node(item, curr);
                        pred.Next = node;
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Removes an item from the list.
        /// Returns true if the item was removed, false if it was not found.
        /// </summary>
        public bool Remove(T item)
        {
            while (true)
            {
                var (pred, curr) = Find(item);
                lock (pred.LockObj)
                {
                    lock (curr.LockObj)
                    {
                        if (!Validate(pred, curr))
                            continue; // retry

                        if (curr.Sentinel || curr.Value.CompareTo(item) != 0)
                            return false; // not present

                        // logical deletion then physical unlink
                        curr.Marked = true;
                        pred.Next = curr.Next;
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the list contains the specified item.
        /// </summary>
        public bool Contains(T item)
        {
            while (true)
            {
                var (pred, curr) = Find(item);
                lock (pred.LockObj)
                {
                    lock (curr.LockObj)
                    {
                        if (!Validate(pred, curr))
                            continue; // retry

                        return !curr.Sentinel && !curr.Marked && curr.Value.CompareTo(item) == 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a snapshot of the list contents in sorted order.
        /// Used only for testing; not part of the concurrent API.
        /// </summary>
        public IList<T> ToList()
        {
            var result = new List<T>();
            var node = _head.Next;
            while (!node.Sentinel)
            {
                if (!node.Marked)
                    result.Add(node.Value);
                node = node.Next;
            }
            return result;
        }

        // ---------- internal helpers ----------

        // Optimistic traversal without acquiring locks.
        private (Node pred, Node curr) Find(T item)
        {
            Node pred = _head;
            Node curr = pred.Next;
            while (!curr.Sentinel && curr.Value.CompareTo(item) < 0)
            {
                pred = curr;
                curr = curr.Next;
            }
            return (pred, curr);
        }

        // Validation after acquiring locks on pred and curr.
        private static bool Validate(Node pred, Node curr)
        {
            return !pred.Marked && (curr == null || !curr.Marked) && pred.Next == curr;
        }
    }
}
