using System;
using System.Collections.Generic;
using System.Diagnostics;
using RaftDemo;

class Program
{
    static void Main()
    {
        var cluster = new Cluster();
        for (int i = 0; i < 5; i++) cluster.AddNode(new RaftNode(i, cluster));

        // Run simulation ticks until a leader emerges
        int maxTicks = 1000;
        RaftNode? leader = null;
        for (int t = 0; t < maxTicks; t++)
        {
            cluster.TickAll();
            leader = cluster.GetLeader();
            if (leader != null) break;
        }
        Debug.Assert(leader != null, "Leader should be elected");

        // Propose a command
        bool ok = leader!.Propose("set x=1");
        Debug.Assert(ok, "Leader should accept proposal");

        // Run a few more ticks to allow replication
        for (int i = 0; i < 200; i++) cluster.TickAll();

        // Verify all nodes have the command in their logs
        foreach (var node in cluster.Nodes)
        {
            Debug.Assert(node.Log.Count == 1, $"Node {node.Id} should have 1 log entry");
            Debug.Assert(node.Log[0].Command == "set x=1", $"Node {node.Id} command mismatch");
            Debug.Assert(node.CommitIndex == 0, $"Node {node.Id} commit index should be 0");
        }

        Console.WriteLine("Raft simulation succeeded. Leader: Node " + leader!.Id);
    }
}
