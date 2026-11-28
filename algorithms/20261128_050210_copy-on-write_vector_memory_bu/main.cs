using System;
using CowVectorNS;

namespace CowVectorTest
{
    internal static class Program
    {
        static void Main()
        {
            TestEmptyVector();
            TestAddAndIndexer();
            TestCloneIsolation();
            TestRemoveAt();
            TestCapacityGrowth();
            TestMultipleClones();
            Console.WriteLine("All CowVector tests passed.");
        }

        static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void TestEmptyVector()
        {
            var v = new CowVector<int>();
            Assert(v.Count == 0, "Empty vector should have Count 0");
        }

        static void TestAddAndIndexer()
        {
            var v = new CowVector<string>();
            v.Add("a");
            v.Add("b");
            v.Add("c");
            Assert(v.Count == 3, "Count after adds");
            Assert(v[0] == "a" && v[1] == "b" && v[2] == "c", "Indexer values");
            v[1] = "beta";
            Assert(v[1] == "beta", "Setter works");
        }

        static void TestCloneIsolation()
        {
            var original = new CowVector<int>();
            for (int i = 0; i < 5; i++) original.Add(i);
            var clone = original.Clone();

            // Both share buffer now; modify clone and ensure original unchanged.
            clone[2] = 99;
            Assert(clone[2] == 99, "Clone modification");
            Assert(original[2] == 2, "Original unchanged after clone write");
            // Ensure buffers are distinct after write.
            Assert(clone.Count == original.Count, "Counts equal after clone write");
        }

        static void TestRemoveAt()
        {
            var v = new CowVector<char>();
            foreach (var ch in new[] { 'x', 'y', 'z' }) v.Add(ch);
            v.RemoveAt(1); // remove 'y'
            Assert(v.Count == 2, "Count after remove");
            Assert(v[0] == 'x' && v[1] == 'z', "Elements after remove");
        }

        static void TestCapacityGrowth()
        {
            var v = new CowVector<int>();
            int initialCap = 4; // known from implementation
            for (int i = 0; i < initialCap + 2; i++) v.Add(i);
            Assert(v.Count == initialCap + 2, "Count after exceeding initial capacity");
            // Verify all values are correct
            for (int i = 0; i < v.Count; i++) Assert(v[i] == i, "Value after growth");
        }

        static void TestMultipleClones()
        {
            var baseVec = new CowVector<double>();
            for (int i = 0; i < 10; i++) baseVec.Add(i * 0.5);
            var c1 = baseVec.Clone();
            var c2 = baseVec.Clone();

            c1[0] = 100.0;
            c2[1] = 200.0;

            Assert(baseVec[0] == 0.0 && baseVec[1] == 0.5, "Base unchanged after multiple clone writes");
            Assert(c1[0] == 100.0 && c1[1] == 0.5, "c1 correct");
            Assert(c2[0] == 0.0 && c2[1] == 200.0, "c2 correct");
        }
    }
}
