using System;
using System.Collections.Generic;

namespace GossipDetector
{
    internal struct HeartbeatInfo
    {
        public int Counter;
        public double Timestamp;
    }

    public sealed class GossipFailureDetector
    {
        private readonly double _failureTimeout;
        private double _time;
        private readonly Dictionary<string, Dictionary<string, HeartbeatInfo>> _views;

        public GossipFailureDetector(double failureTimeoutSeconds)
        {
            if (failureTimeoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(failureTimeoutSeconds));
            _failureTimeout = failureTimeoutSeconds;
            _time = 0.0;
            _views = new Dictionary<string, Dictionary<string, HeartbeatInfo>>();
        }

        public double CurrentTime => _time;

        public void RegisterNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node identifier cannot be null or whitespace.", nameof(nodeId));
            if (_views.ContainsKey(nodeId))
                throw new InvalidOperationException($"Node '{nodeId}' is already registered.");

            var view = new Dictionary<string, HeartbeatInfo>();
            view[nodeId] = new HeartbeatInfo { Counter = 0, Timestamp = _time };
            _views[nodeId] = view;
        }

        public void IncrementHeartbeat(string nodeId)
        {
            var view = GetView(nodeId);
            var info = view[nodeId];
            info.Counter++;
            info.Timestamp = _time;
            view[nodeId] = info;
        }

        public void Gossip(string fromNodeId, string toNodeId)
        {
            var fromView = GetView(fromNodeId);
            var toView = GetView(toNodeId);

            foreach (var kvp in fromView)
            {
                var target = kvp.Key;
                var incoming = kvp.Value;

                if (!toView.TryGetValue(target, out var existing) || incoming.Counter > existing.Counter)
                {
                    var merged = new HeartbeatInfo
                    {
                        Counter = incoming.Counter,
                        Timestamp = _time
                    };
                    toView[target] = merged;
                }
            }
        }

        public List<string> SuspectFailedNodes(string nodeId)
        {
            var view = GetView(nodeId);
            var suspects = new List<string>();

            foreach (var kvp in view)
            {
                if (kvp.Key == nodeId)
                    continue;

                if (_time - kvp.Value.Timestamp > _failureTimeout)
                    suspects.Add(kvp.Key);
            }

            return suspects;
        }

        public void Tick(double seconds)
        {
            if (seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _time += seconds;
        }

        public int GetCounter(string nodeId)
        {
            var view = GetView(nodeId);
            return view[nodeId].Counter;
        }

        private Dictionary<string, HeartbeatInfo> GetView(string nodeId)
        {
            if (!_views.TryGetValue(nodeId, out var view))
                throw new InvalidOperationException($"Node '{nodeId}' is not registered.");
            return view;
        }
    }
}
