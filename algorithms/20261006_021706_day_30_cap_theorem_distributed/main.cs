using System;
using System.Collections.Generic;
using System.Diagnostics;
using CapSimulation;

namespace CapSimulationDemo
{
    internal static class Program
    {
        static void Main()
        {
            // Setup a distributed store with three nodes
            var nodes = new List<Node>
            {
                new Node(0),
                new Node(1),
                new Node(2)
            };
            var store = new DistributedStore(nodes);

            // ---------- Strong Consistency Baseline ----------
            CapSimulator.Write(store, "x", "initial", ConsistencyLevel.Strong);
            var readStrong = CapSimulator.Read(store, "x", ConsistencyLevel.Strong);
            Debug.Assert(readStrong.Value == "initial", "Strong read should return 'initial'.");

            // ---------- Simulate Partition ----------
            // Partition: only node 0 is reachable
            store.SetReachableNodes(new[] { 0 });

            // Strong write should fail
            bool strongWriteFailed = false;
            try
            {
                CapSimulator.Write(store, "x", "partitioned", ConsistencyLevel.Strong);
            }
            catch (PartitionException)
            {
                strongWriteFailed = true;
            }
            Debug.Assert(strongWriteFailed, "Strong write must fail during partition.");

            // Eventual write should succeed on reachable node(s)
            CapSimulator.Write(store, "x", "eventual1", ConsistencyLevel.Eventual);
            var readEventualDuringPartition = CapSimulator.Read(store, "x", ConsistencyLevel.Eventual);
            Debug.Assert(readEventualDuringPartition.Value == "eventual1", "Eventual read during partition should see latest write.");

            // Verify that unreachable nodes still hold old value
            var node1Value = nodes[1].Read("x")?.Value;
            var node2Value = nodes[2].Read("x")?.Value;
            Debug.Assert(node1Value == "initial", "Unreachable node should retain previous value.");
            Debug.Assert(node2Value == "initial", "Unreachable node should retain previous value.");

            // ---------- Heal Partition ----------
            store.SetReachableNodes(new[] { 0, 1, 2 });
            // Synchronize nodes to achieve eventual consistency convergence
            store.SyncAllNodes();

            // After sync, all nodes should have the latest eventual value
            foreach (var node in store.Nodes)
            {
                var vv = node.Read("x");
                Debug.Assert(vv != null && vv.Value == "eventual1", "All nodes must converge to 'eventual1' after healing.");
            }

            // Strong read should now succeed and return the converged value
            var readStrongAfterHeal = CapSimulator.Read(store, "x", ConsistencyLevel.Strong);
            Debug.Assert(readStrongAfterHeal.Value == "eventual1", "Strong read after healing should return the latest converged value.");

            // ---------- Additional Edge Cases ----------
            // Write with eventual consistency when all nodes are reachable
            CapSimulator.Write(store, "y", "valueY", ConsistencyLevel.Eventual);
            var readY = CapSimulator.Read(store, "y", ConsistencyLevel.Eventual);
            Debug.Assert(readY.Value == "valueY", "Eventual write/read with full connectivity should succeed.");

            // Strong read of a non‑existent key should throw
            bool keyNotFound = false;
            try
            {
                CapSimulator.Read(store, "nonexistent", ConsistencyLevel.Strong);
            }
            catch (KeyNotFoundException)
            {
                keyNotFound = true;
            }
            Debug.Assert(keyNotFound, "Reading a missing key must throw KeyNotFoundException.");

            Console.WriteLine("All CAP theorem simulation assertions passed.");
        }
    }
}
