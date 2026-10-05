using System;
using System.Collections.Generic;

namespace CAPSimulation
{
    public enum ConsistencyModel
    {
        Strong,
        Eventual
    }

    public class Partition
    {
        public HashSet<int> AffectedNodeIds { get; }

        public Partition(IEnumerable<int> nodeIds)
        {
            AffectedNodeIds = new HashSet<int>(nodeIds);
        }

        // Returns true if communication between the two nodes is blocked by this partition.
        public bool IsPartitioned(int nodeId1, int nodeId2)
        {
            bool inA = AffectedNodeIds.Contains(nodeId1);
            bool inB = AffectedNodeIds.Contains(nodeId2);
            return inA != inB;
        }
    }

    public interface IDistributedNode
    {
        int Id { get; }
        void ApplyWrite(string key, string value, long timestamp);
        (string value, long timestamp) ReadLocal(string key);
        bool CanCommunicateWith(int otherNodeId, IEnumerable<Partition> partitions);
    }

    public class UnavailableException : Exception
    {
        public UnavailableException(string message) : base(message) { }
    }
}
