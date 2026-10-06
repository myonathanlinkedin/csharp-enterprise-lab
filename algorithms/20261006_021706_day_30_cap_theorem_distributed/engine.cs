using System;
using System.Collections.Generic;
using System.Linq;

namespace CapSimulation
{
    public static class CapSimulator
    {
        public static void Write(DistributedStore store, string key, string value, ConsistencyLevel level)
        {
            if (level == ConsistencyLevel.Strong)
            {
                if (!store.IsFullyConnected)
                    throw new PartitionException("Strong consistency cannot be guaranteed during a network partition.");

                var version = store.NextVersion();
                foreach (var node in store.Nodes)
                {
                    node.Write(key, value, version);
                }
            }
            else // Eventual
            {
                var version = store.NextVersion();
                foreach (var node in store.ReachableNodes)
                {
                    node.Write(key, value, version);
                }
            }
        }

        public static VersionedValue Read(DistributedStore store, string key, ConsistencyLevel level)
        {
            if (level == ConsistencyLevel.Strong)
            {
                if (!store.IsFullyConnected)
                    throw new PartitionException("Strong consistency cannot be guaranteed during a network partition.");

                var reads = store.Nodes
                    .Select(node => node.Read(key))
                    .Where(v => v != null)
                    .ToList();

                if (reads.Count == 0)
                    throw new KeyNotFoundException($"Key '{key}' not found on any node.");

                var firstVersion = reads[0]!.Version;
                if (reads.Any(v => v!.Version != firstVersion))
                    throw new InvalidOperationException("Strong consistency violation: divergent versions detected.");

                return reads[0]!;
            }
            else // Eventual
            {
                foreach (var node in store.ReachableNodes)
                {
                    var vv = node.Read(key);
                    if (vv != null)
                        return vv;
                }
                throw new KeyNotFoundException($"Key '{key}' not found on any reachable node.");
            }
        }
    }
}
