using System;
using CRDT;

static class TestRunner
{
    private static int _passed = 0;
    private static int _failed = 0;

    private static void Assert(bool condition, string message = "")
    {
        if (!condition)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {message}");
        }
        else
        {
            _passed++;
        }
    }

    private static void Report()
    {
        Console.WriteLine($"Tests passed: {_passed}, failed: {_failed}");
        if (_failed > 0) Environment.Exit(1);
    }

    static void Main()
    {
        TestBasicOperations();
        TestMergeCommutativity();
        TestMergeIdempotence();
        TestMergeAssociativity();
        TestNegativeValues();
        Report();
    }

    private static void TestBasicOperations()
    {
        var a = new PNCounter("A");
        a.Increment();          // +1
        a.Increment(4);         // +4 => total +5
        a.Decrement(2);         // -2 => net +3
        Assert(a.Value == 3, "Basic increment/decrement failed.");
    }

    private static void TestMergeCommutativity()
    {
        var a = new PNCounter("A");
        var b = new PNCounter("B");

        a.Increment(3);
        b.Decrement(1);
        b.Increment(2);

        var aCopy = a.Clone();
        var bCopy = b.Clone();

        a.Merge(b);
        bCopy.Merge(aCopy); // bCopy now holds the merge result of (b ⊔ a)

        Assert(a.Value == bCopy.Value, "Merge commutativity value mismatch.");
        Assert(a.Value == 4, "Merge commutativity expected value 4.");
    }

    private static void TestMergeIdempotence()
    {
        var a = new PNCounter("A");
        a.Increment(5);
        a.Decrement(2);

        var before = a.Value;
        a.Merge(a.Clone()); // merge with itself
        Assert(a.Value == before, "Merge idempotence altered value.");
    }

    private static void TestMergeAssociativity()
    {
        var a = new PNCounter("A");
        var b = new PNCounter("B");
        var c = new PNCounter("C");

        a.Increment(1);
        b.Increment(2);
        c.Decrement(1);
        c.Increment(4);

        var left = a.Clone();
        left.Merge(b);
        left.Merge(c); // (a ⊔ b) ⊔ c

        var right = b.Clone();
        right.Merge(c);
        right.Merge(a); // a ⊔ (b ⊔ c)

        Assert(left.Value == right.Value, "Merge associativity value mismatch.");
        Assert(left.Value == 6, "Merge associativity expected value 6.");
    }

    private static void TestNegativeValues()
    {
        var a = new PNCounter("A");
        a.Decrement(7);
        Assert(a.Value == -7, "Negative value after decrement failed.");

        var b = new PNCounter("B");
        b.Increment(3);
        a.Merge(b);
        Assert(a.Value == -4, "Merge with negative base failed.");
    }
}
