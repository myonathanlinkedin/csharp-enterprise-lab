using System;
using System.Collections.Generic;

namespace GossipFailureDetectorLib
{
    internal sealed class NodeInfo
    {
        public string Id { get; }
        public long Counter { get; private set; }
        public DateTime LastSeen { get; private set; }

        public NodeInfo(string id, long counter, DateTime lastSeen)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Counter = counter;
            LastSeen = lastSeen;
        }

        public void UpdateCounter(long newCounter, DateTime timestamp)
        {
            if (newCounter > Counter)
            {
                Counter = newCounter;
                LastSeen = timestamp;
            }
            else if (newCounter == Counter && timestamp > LastSeen)
            {
                // Same counter but newer observation
                LastSeen = timestamp;
            }
        }

        public void Increment(DateTime timestamp)
        {
            Counter++;
            LastSeen = timestamp;
        }
    }

    public sealed class GossipFailureDetector
    {
        private readonly Dictionary<string, NodeInfo> _nodes;
        private readonly TimeSpan _failureTimeout;

        public GossipFailureDetector(TimeSpan failureTimeout)
        {
            if (failureTimeout <= TimeSpan.Zero)
                throw new ArgumentException("Failure timeout must be positive.", nameof(failureTimeout));

            _nodes = new Dictionary<string, NodeInfo>();
            _failureTimeout = failureTimeout;
        }

        /// <summary>
        /// Registers a node in the detector. If the node already exists, its counter is reset.
        /// </summary>
        public void RegisterNode(string nodeId, DateTime now)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));

            var info = new NodeInfo(nodeId, 0, now);
            _nodes[nodeId] = info;
        }

        /// <summary>
        /// Increments the local heartbeat counter for the given node.
        /// </summary>
        public void IncrementHeartbeat(string nodeId, DateTime now)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));

            if (!_nodes.TryGetValue(nodeId, out var info))
                throw new InvalidOperationException($"Node '{nodeId}' is not registered.");

            info.Increment(now);
        }

        /// <summary>
        /// Generates a gossip payload containing the latest counters for all known nodes.
        /// </summary>
        public Dictionary<string, long> GenerateGossip()
        {
            var payload = new Dictionary<string, long>(_nodes.Count);
            foreach (var kvp in _nodes)
            {
                payload[kvp.Key] = kvp.Value.Counter;
            }
            return payload;
        }

        /// <summary>
        /// Merges a received gossip payload into the local state.
        /// </summary>
        public void ReceiveGossip(Dictionary<string, long> gossip, DateTime now)
        {
            if (gossip == null) throw new ArgumentNullException(nameof(gossip));

            foreach (var kvp in gossip)
            {
                var nodeId = kvp.Key;
                var remoteCounter = kvp.Value;

                if (_nodes.TryGetValue(nodeId, out var localInfo))
                {
                    localInfo.UpdateCounter(remoteCounter, now);
                }
                else
                {
                    // Unknown node – add it with the received counter.
                    var newInfo = new NodeInfo(nodeId, remoteCounter, now);
                    _nodes[nodeId] = newInfo;
                }
            }
        }

        /// <summary>
        /// Returns the set of node identifiers that have not been observed within the failure timeout.
        /// </summary>
        public List<string> DetectFailures(DateTime now)
        {
            var failed = new List<string>();
            foreach (var kvp in _nodes)
            {
                var elapsed = now - kvp.Value.LastSeen;
                if (elapsed > _failureTimeout)
                {
                    failed.Add(kvp.Key);
                }
            }
            return failed;
        }

        /// <summary>
        /// Exposes the internal counter for testing purposes (read‑only).
        /// </summary>
        public long GetCounter(string nodeId)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
            if (!_nodes.TryGetValue(nodeId, out var info))
                throw new InvalidOperationException($"Node '{nodeId}' is not registered.");
            return info.Counter;
        }

        /// <summary>
        /// Exposes the last‑seen timestamp for testing purposes (read‑only).
        /// </summary>
        public DateTime GetLastSeen(string nodeId)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
            if (!_nodes.TryGetValue(nodeId, out var info))
                throw new InvalidOperationException($"Node '{nodeId}' is not registered.");
            return info.LastSeen;
        }
    }
}
