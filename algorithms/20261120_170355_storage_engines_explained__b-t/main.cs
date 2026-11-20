using System;
using System.Collections.Generic;
using System.Diagnostics;
using StorageEngines;

class Program
{
    static void Main()
    {
        TestBTree();
        TestLSMTree();
        Console.WriteLine("All tests passed.");
    }

    static void TestBTree()
    {
        var btree = new BTree<int, string>(minimumDegree: 3);
        var data = new Dictionary<int, string>
        {
            {10,"A"}, {20,"B"}, {5,"C"}, {6,"D"}, {12,"E"},
            {30,"F"}, {7,"G"}, {17,"H"}, {3,"I"}, {4,"J"}
        };
        foreach (var kv in data)
            btree.Insert(kv.Key, kv.Value);

        // Verify all inserted keys are searchable.
        foreach (var kv in data)
        {
            var (found, val) = btree.Search(kv.Key);
            Debug.Assert(found, $"Key {kv.Key} not found in B-Tree.");
            Debug.Assert(val == kv.Value, $"Value mismatch for key {kv.Key}.");
        }

        // Search for non‑existent key.
        var (foundMissing, _) = btree.Search(999);
        Debug.Assert(!foundMissing, "Non‑existent key incorrectly reported as found.");
    }

    static void TestLSMTree()
    {
        var lsm = new LSMTree<int, string>(memtableThreshold: 3, runMergeThreshold: 2);
        // Insert 7 items; this will cause multiple flushes and a merge.
        var entries = new[]
        {
            (1,"one"), (2,"two"), (3,"three"),
            (4,"four"), (5,"five"), (6,"six"),
            (7,"seven")
        };
        foreach (var (k,v) in entries)
            lsm.Insert(k, v);

        // After inserts, memtable should contain the last two items (6,7)
        Debug.Assert(lsm.MemTable.Count == 2, "MemTable size mismatch after flushes.");
        Debug.Assert(lsm.MemTable.ContainsKey(6) && lsm.MemTable.ContainsKey(7));

        // There should be exactly one merged run (containing keys 1‑5)
        Debug.Assert(lsm.Runs.Count == 1, "Run count after merge incorrect.");
        var run = lsm.Runs[0];
        Debug.Assert(run.Count == 5, "Merged run size incorrect.");
        for (int i = 0; i < 5; i++)
            Debug.Assert(run[i].Key == i + 1, "Run key order incorrect.");

        // Verify lookups across memtable and runs.
        foreach (var (k, v) in entries)
        {
            var (found, val) = lsm.Search(k);
            Debug.Assert(found, $"Key {k} not found in LSM Tree.");
            Debug.Assert(val == v, $"Value mismatch for key {k}.");
        }

        // Search for missing key.
        var (foundMissing, _) = lsm.Search(999);
        Debug.Assert(!foundMissing, "Non‑existent key incorrectly reported as found in LSM Tree.");
    }
}
