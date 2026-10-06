using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydro
{
    public class Engine
    {
        public async Task Start()
        {
            Node node1 = new Node();
            Node node2 = new Node();

            await node1.SendMessage("Hello from node1");
            await node2.SendMessage("Hello from node2");
        }
    }
}
