using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydro
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Engine engine = new Engine();

            await engine.Start();
        }
    }
}
