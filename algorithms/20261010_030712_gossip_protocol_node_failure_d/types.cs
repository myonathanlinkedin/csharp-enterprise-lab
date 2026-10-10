using System.Collections.Generic;

namespace GossipFailureDetector
{
    public enum NodeState
    {
        Alive,
        Suspect,
        Failed
    }

    public sealed class Node
    {
        public int Id { get; }
        public NodeState State { get; set; }
        public bool IsActive { get; set; } // false => node stops sending heartbeats
        public int LastHeartbeat { get; set; } // step number of node's own last heartbeat
        public Dictionary<int, int> LastSeen { get; } // peerId -> step of last heartbeat received

        public Node(int id, int totalNodes)
        {
            Id = id;
            State = NodeState.Alive;
            IsActive = true;
            LastHeartbeat = 0;
            LastSeen = new Dictionary<int, int>(totalNodes - 1);
            // Initialise LastSeen for all other nodes with 0 (no heartbeat yet)
            for (int i = 0; i < totalNodes; i++)
            {
                if (i != id)
                {
                    LastSeen[i] = 0;
                }
            }
        }
    }

    public sealed class GossipMessage
    {
        public int FromId { get; }
        public int Counter { get; } // step number of the heartbeat

        public GossipMessage(int fromId, int counter)
        {
            FromId = fromId;
            Counter = counter;
        }
    }
}
