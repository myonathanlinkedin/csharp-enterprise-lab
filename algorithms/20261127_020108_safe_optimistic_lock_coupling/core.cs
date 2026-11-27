using System;
using System.Collections.Generic;

namespace OptimisticLockCoupling
{
    // Node of the linked list
    internal sealed class Node<TKey, TValue> where TKey : IComparable<TKey>
    {
        public readonly TKey Key;
        public TValue Value;
        public volatile Node<TKey, TValue>? Next;
        public volatile bool Marked; // logical deletion flag
        public readonly object Lock = new object();

        public Node(TKey key, TValue value, Node<TKey, TValue>? next = null)
        {
            Key = key;
            Value = value;
            Next = next;
            Marked = false;
        }

        // Sentinel constructor
        public Node()
        {
            Key = default!;
            Value = default!;
            Next = null;
            Marked = false;
        }
    }

    // Optimistic Lock Coupling sorted linked list
    public sealed class OptimisticLinkedList<TKey, TValue> where TKey : IComparable<TKey>
    {
        private readonly Node<TKey, TValue> _head; // sentinel head (no key)

        public OptimisticLinkedList()
        {
            _head = new Node<TKey, TValue>(); // head sentinel
        }

        // Public API ---------------------------------------------------------

        // Try to add a key/value pair. Returns false if key already exists.
        public bool TryAdd(TKey key, TValue value)
        {
            while (true)
            {
                // 1. Locate position without locks
                Node<TKey, TValue> pred = _head;
                Node<TKey, TValue>? curr = pred.Next;
                while (curr != null && curr.Key.CompareTo(key) < 0)
                {
                    pred = curr;
                    curr = curr.Next;
                }

                // 2. Acquire locks in key order (pred before curr)
                lock (pred.Lock)
                {
                    if (curr != null)
                    {
                        lock (curr.Lock)
                        {
                            if (!Validate(pred, curr))
                                continue; // retry

                            // Duplicate?
                            if (curr.Key.CompareTo(key) == 0)
                                return false;

                            // Insert new node
                            var node = new Node<TKey, TValue>(key, value, curr);
                            pred.Next = node;
                            return true;
                        }
                    }
                    else
                    {
                        // curr is null => insert at tail
                        if (!Validate(pred, null))
                            continue; // retry

                        var node = new Node<TKey, TValue>(key, value, null);
                        pred.Next = node;
                        return true;
                    }
                }
            }
        }

        // Try to remove a key. Returns false if key not present.
        public bool TryRemove(TKey key, out TValue? value)
        {
            value = default;
            while (true)
            {
                Node<TKey, TValue> pred = _head;
                Node<TKey, TValue>? curr = pred.Next;
                while (curr != null && curr.Key.CompareTo(key) < 0)
                {
                    pred = curr;
                    curr = curr.Next;
                }

                if (curr == null || curr.Key.CompareTo(key) != 0)
                    return false; // not found

                lock (pred.Lock)
                {
                    lock (curr.Lock)
                    {
                        if (!Validate(pred, curr))
                            continue; // retry

                        // Logical deletion
                        curr.Marked = true;
                        // Physical removal
                        pred.Next = curr.Next;
                        value = curr.Value;
                        return true;
                    }
                }
            }
        }

        // Try to get the value associated with a key. Optimistic read.
        public bool TryGetValue(TKey key, out TValue? value)
        {
            value = default;
            while (true)
            {
                Node<TKey, TValue> pred = _head;
                Node<TKey, TValue>? curr = pred.Next;
                while (curr != null && curr.Key.CompareTo(key) < 0)
                {
                    pred = curr;
                    curr = curr.Next;
                }

                if (curr == null)
                    return false; // reached end, not found

                // Snapshot the candidate node
                if (curr.Key.CompareTo(key) == 0 && !curr.Marked)
                {
                    TValue candidate = curr.Value;

                    // Validation phase (no locks)
                    if (pred.Next == curr && !pred.Marked && !curr.Marked)
                    {
                        value = candidate;
                        return true;
                    }
                    // else retry
                }
                else
                {
                    // Key not present
                    return false;
                }
            }
        }

        // Enumerate all key/value pairs in order (used for testing)
        public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate()
        {
            Node<TKey, TValue>? curr = _head.Next;
            while (curr != null)
            {
                if (!curr.Marked)
                    yield return new KeyValuePair<TKey, TValue>(curr.Key, curr.Value);
                curr = curr.Next;
            }
        }

        // --------------------------------------------------------------------

        // Validate that pred still points to curr and neither is marked.
        private static bool Validate(Node<TKey, TValue> pred, Node<TKey, TValue>? curr)
        {
            if (pred.Marked)
                return false;
            if (curr != null && curr.Marked)
                return false;
            return pred.Next == curr;
        }
    }
}
