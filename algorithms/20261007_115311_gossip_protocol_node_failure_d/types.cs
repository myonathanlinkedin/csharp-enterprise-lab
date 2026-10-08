using System;
using System.Collections.Generic;

namespace GossipFailureDetector
{
    public enum NodeStatus
    {
        Alive,
        Suspected
    }

    public sealed class Node
    {
        public Guid Id { get; }

        private long _heartbeat;
        private readonly Dictionary<Guid, long> _peerHeartbeats = new();
        private readonly Dictionary<Guid, DateTime> _lastSeen = new();
        private readonly HashSet<Guid> _suspected = new();

        public Node()
        {
            Id = Guid.NewGuid();
            _heartbeat = 0;
        }

        // Increment this node's own heartbeat (simulating work)
        public void Tick()
        {
            _heartbeat++;
        }

        // Called when this node receives gossip from a peer
        public void ReceiveGossip(Node peer, DateTime now)
        {
            // Update last seen timestamp for the peer
            _lastSeen[peer.Id] = now;

            // Merge heartbeat information
            if (!_peerHeartbeats.ContainsKey(peer.Id) || peer._heartbeat > _peerHeartbeats[peer.Id])
            {
                _peerHeartbeats[peer.Id] = peer._heartbeat;
            }

            // Also merge peer's view of other nodes
            foreach (var kvp in peer._peerHeartbeats)
            {
                if (kvp.Key == Id) continue; // skip self
                if (!_peerHeartbeats.ContainsKey(kvp.Key) || kvp.Value > _peerHeartbeats[kvp.Key])
                {
                    _peerHeartbeats[kvp.Key] = kvp.Value;
                }
            }
        }

        // Called by the engine to evaluate failure suspicion based on timeout
        public void EvaluateSuspicions(TimeSpan timeout, DateTime now)
        {
            var toSuspect = new List<Guid>();
            foreach (var kvp in _lastSeen)
            {
                if (now - kvp.Value > timeout)
                {
                    toSuspect.Add(kvp.Key);
                }
            }

            foreach (var id in toSuspect)
            {
                _suspected.Add(id);
            }
        }

        // Returns the current status of a given peer
        public NodeStatus GetPeerStatus(Guid peerId)
        {
            return _suspected.Contains(peerId) ? NodeStatus.Suspected : NodeStatus.Alive;
        }

        // Exposes known peers (including self) for testing
        public IReadOnlyCollection<Guid> KnownPeers => _peerHeartbeats.Keys;

        // Exposes suspected peers for testing
        public IReadOnlyCollection<Guid> SuspectedPeers => _suspected;
    }
}
