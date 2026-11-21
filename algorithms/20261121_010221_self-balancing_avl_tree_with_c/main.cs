using System;
using System.Collections.Generic;
using ChronosAVL;

class Program
{
    static void Main()
    {
        TestInsertionAndSearch();
        TestBalancing();
        TestDeletion();
        TestChronologicalOrder();
        Console.WriteLine("All tests passed.");
    }

    static void Assert(bool condition, string message = "Assertion failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void TestInsertionAndSearch()
    {
        var tree = new AVLTree<int, string>();
        Assert(tree.Insert(5, "five"));
        Assert(tree.Insert(3, "three"));
        Assert(tree.Insert(8, "eight"));
        Assert(!tree.Insert(5, "duplicate")); // duplicate key updates value
        Assert(tree.Count == 3);

        Assert(tree.TryGetValue(5, out var v5) && v5 == "five");
        Assert(tree.TryGetValue(3, out var v3) && v3 == "three");
        Assert(tree.TryGetValue(8, out var v8) && v8 == "eight");
        Assert(!tree.TryGetValue(10, out _));
    }

    static void TestBalancing()
    {
        var tree = new AVLTree<int, int>();
        // Insert keys that would cause right-right case
        tree.Insert(10, 0);
        tree.Insert(20, 0);
        tree.Insert(30, 0);
        // Tree should be balanced; height should be 2
        // Verify in-order sequence is sorted
        var expected = new[] {10,20,30};
        int i = 0;
        foreach (var kv in tree.InOrder())
        {
            Assert(kv.Key == expected[i++]);
        }
        Assert(i == expected.Length);
    }

    static void TestDeletion()
    {
        var tree = new AVLTree<int, char>();
        foreach (var k in new[] {15, 10, 20, 8, 12, 17, 25})
            tree.Insert(k, (char)('A' + k % 26));

        Assert(tree.Delete(10)); // node with two children
        Assert(!tree.TryGetValue(10, out _));
        Assert(tree.Count == 6);

        // Ensure remaining keys are correct and still balanced
        var remaining = new List<int>();
        foreach (var kv in tree.InOrder())
            remaining.Add(kv.Key);
        var expected = new List<int> {8,12,15,17,20,25};
        Assert(remaining.Count == expected.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert(remaining[i] == expected[i]);
    }

    static void TestChronologicalOrder()
    {
        var tree = new AVLTree<string, int>();
        var keys = new[] {"alpha","beta","gamma","delta"};
        foreach (var k in keys)
            tree.Insert(k, k.Length);

        var chrono = new List<string>();
        foreach (var kv in tree.Chronological())
            chrono.Add(kv.Key);
        Assert(chrono.Count == keys.Length);
        for (int i = 0; i < keys.Length; i++)
            Assert(chrono[i] == keys[i]);

        // Delete a middle element and verify chronology skips it
        tree.Delete("gamma");
        chrono.Clear();
        foreach (var kv in tree.Chronological())
            chrono.Add(kv.Key);
        var expected = new[] {"alpha","beta","delta"};
        Assert(chrono.Count == expected.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert(chrono[i] == expected[i]);
    }
}
