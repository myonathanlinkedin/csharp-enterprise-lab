using System;
using System.Threading.Tasks;
using ActorModel;

namespace ActorModelTest
{
    class Program
    {
        static async Task Main()
        {
            await TestCounterActor();
            await TestPingPong();
            Console.WriteLine("All tests passed.");
        }

        private static async Task TestCounterActor()
        {
            var counter = new CounterActor();

            const int messages = 1000;
            for (int i = 0; i < messages; i++)
                counter.Send(1);

            // Allow processing time.
            await Task.Delay(200);
            await counter.DisposeAsync();

            if (counter.Count != messages)
                throw new InvalidOperationException($"Counter mismatch: expected {messages}, got {counter.Count}");
        }

        private static async Task TestPingPong()
        {
            // Create actors with circular references.
            PingActor ping = null!;
            PongActor pong = null!;

            ping = new PingActor(pong);
            pong = new PongActor(ping);

            // Rewire after both are instantiated.
            ping = new PingActor(pong);
            pong = new PongActor(ping);

            // Start interaction.
            ping.Send("start");

            // Wait for completion or timeout.
            var completed = await Task.WhenAny(ping.Completion, Task.Delay(1000));
            if (completed != ping.Completion)
                throw new TimeoutException("Ping-Pong interaction timed out.");

            await ping.DisposeAsync();
            await pong.DisposeAsync();
        }
    }
}
