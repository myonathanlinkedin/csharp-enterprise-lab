using System;

namespace MemorySafeTech
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception("Assertion failed: " + message);
        }

        private static void RunAllTests()
        {
            TestSimpleReferenceLifecycle();
            TestCycleWithoutExternalRoot();
            TestCycleWithExternalRoot();
            TestMultipleRootsAndSharedSubgraph();
            Console.WriteLine("All tests passed.");
        }

        private static void TestSimpleReferenceLifecycle()
        {
            var mgr = new MemoryManager();
            var a = mgr.CreateNode(1);
            mgr.AddExternalReference(1);
            Assert(mgr.NodeCount == 1, "Node count after creation");

            mgr.ReleaseExternalReference(1);
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 0, "Node should be collected after external release");
        }

        private static void TestCycleWithoutExternalRoot()
        {
            var mgr = new MemoryManager();
            var a = mgr.CreateNode(1);
            var b = mgr.CreateNode(2);
            mgr.AddInternalReference(1, 2);
            mgr.AddInternalReference(2, 1);
            // No external references
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 0, "Both nodes in a cycle without roots should be collected");
        }

        private static void TestCycleWithExternalRoot()
        {
            var mgr = new MemoryManager();
            var a = mgr.CreateNode(1);
            var b = mgr.CreateNode(2);
            mgr.AddInternalReference(1, 2);
            mgr.AddInternalReference(2, 1);
            mgr.AddExternalReference(1); // root on node 1
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 2, "Cycle with external root must survive");

            // Release root and collect again
            mgr.ReleaseExternalReference(1);
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 0, "After releasing root, cycle should be collected");
        }

        private static void TestMultipleRootsAndSharedSubgraph()
        {
            var mgr = new MemoryManager();
            var root1 = mgr.CreateNode(1);
            var root2 = mgr.CreateNode(2);
            var shared = mgr.CreateNode(3);
            var leaf = mgr.CreateNode(4);

            mgr.AddExternalReference(1);
            mgr.AddExternalReference(2);

            mgr.AddInternalReference(1, 3);
            mgr.AddInternalReference(2, 3);
            mgr.AddInternalReference(3, 4);

            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 4, "All nodes reachable from roots should survive");

            // Drop one root
            mgr.ReleaseExternalReference(1);
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 4, "Shared subgraph still reachable from root2");

            // Drop second root
            mgr.ReleaseExternalReference(2);
            mgr.CollectGarbage();
            Assert(mgr.NodeCount == 0, "All nodes should be collected after all roots released");
        }

        static void Main()
        {
            RunAllTests();
        }
    }
}
