using System;
using System.Threading;
using EtcdDemo;

class Program
{
    static void Main()
    {
        RunAllTests();
        Console.WriteLine("All tests passed.");
    }

    static void RunAllTests()
    {
        TestPutGet();
        TestDelete();
        TestCompareAndSwap();
        TestLeaseExpiration();
        TestWatch();
    }

    static void Assert(bool condition, string message = "Assertion failed")
    {
        if (!condition) throw new Exception(message);
    }

    static void TestPutGet()
    {
        using var store = new EtcdStore();
        var rev = store.Put("foo", "bar");
        var result = store.Get("foo");
        Assert(result.HasValue, "Get should return a value");
        Assert(result.Value.Value == "bar", "Value mismatch");
        Assert(result.Value.Revision == rev, "Revision mismatch");
    }

    static void TestDelete()
    {
        using var store = new EtcdStore();
        store.Put("key", "val");
        var deleted = store.Delete("key");
        Assert(deleted, "Delete should succeed");
        var result = store.Get("key");
        Assert(!result.HasValue, "Key should be gone after delete");
    }

    static void TestCompareAndSwap()
    {
        using var store = new EtcdStore();
        store.Put("k", "v1");
        var success = store.CompareAndSwap("k", "v1", "v2");
        Assert(success, "CAS should succeed with matching expected value");
        var fail = store.CompareAndSwap("k", "v1", "v3");
        Assert(!fail, "CAS should fail when expected value does not match");
        var cur = store.Get("k");
        Assert(cur.HasValue && cur.Value.Value == "v2", "Value should be updated to v2");
    }

    static void TestLeaseExpiration()
    {
        using var store = new EtcdStore();
        var leaseId = store.CreateLease(TimeSpan.FromMilliseconds(300));
        store.Put("temp", "data", leaseId);
        var before = store.Get("temp");
        Assert(before.HasValue, "Key should exist before lease expires");
        Thread.Sleep(500); // wait beyond ttl
        var after = store.Get("temp");
        Assert(!after.HasValue, "Key should be removed after lease expiration");
    }

    static void TestWatch()
    {
        using var store = new EtcdStore();
        int callbackCount = 0;
        using var sub = store.Watch("watched", (k, v) => { Interlocked.Increment(ref callbackCount); });
        store.Put("watched", "first");
        store.Put("watched", "second");
        store.Delete("watched");
        // Allow callbacks to run (they are synchronous in this implementation)
        Assert(callbackCount == 3, $"Expected 3 callbacks, got {callbackCount}");
    }
}
