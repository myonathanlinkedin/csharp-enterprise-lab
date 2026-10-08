using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydro
{
    public class Node
    {
        public async Task SendMessage(string message)
        {
            Console.WriteLine($"{this.Address}: Sending message: {message}");
        }

        public async Task<string> ReceiveMessage()
        {
            Console.WriteLine($"{this.Address}: Receiving message");
            return "Message received";
        }

        public string Address { get; set; }
    }
}
