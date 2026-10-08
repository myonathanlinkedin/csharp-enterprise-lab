using System;

namespace Hydro
{
    public enum NodeType
    {
        Master,
        Worker
    }

    public struct Message
    {
        public NodeType NodeType;
        public string Data;
    }
}
