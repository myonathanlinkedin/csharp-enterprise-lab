using System;
using CAPSimulation;

class Program
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Assertion failed: " + message);
    }

    static void Main()
    {
        // Scenario 1: No partition, strong consistency should succeed.
        var system1 = new DistributedSystem(nodeCount: 5);
        system1.Write("x", "1", ConsistencyModel.Strong);
        var read1 = system1.Read("x", ConsistencyModel.Strong);
        Assert(read1 == "1", "Strong read without partition should return latest value.");

        // Scenario 2: Partition that breaks quorum, strong consistency should be unavailable.
        var system2 = new DistributedSystem(nodeCount: 5);
        system2.AddPartition(new Partition(new[] { 0, 1, 2 }));
        try
        {
            system2.Write("y", "A", ConsistencyModel.Strong);
            Assert(false, "Write should have thrown UnavailableException due to lack of quorum.");
        }
        catch (UnavailableException) { }

        // Scenario 3: Eventual consistency remains available despite partition
}
}
