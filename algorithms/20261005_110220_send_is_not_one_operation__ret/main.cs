using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DistributedMessaging;

class Program
{
    static async Task Main()
    {
        await RunAllTests();
        Console.WriteLine("All tests passed.");
    }

    static async Task RunAllTests()
    {
        await TestBasicSend();
        await TestRetryOnTransientFailure();
        await BenchmarkThroughput(100_000);
    }

    static async Task TestBasicSend()
    {
        var transport = new InMemoryTransport();
        var receiver = new SimpleReceiver();
        transport.RegisterReceiver(receiver);

        // Ack function checks receiver storage
        Func<IMessage, Task<bool>> ackFunc = async msg =>
        {
            await Task.Yield();
            return receiver.Received.ContainsKey(msg.Id);
        };

        var pipeline = new SendPipeline(new IMessageStage[]
        {
            new SerializationStage(),
            new TransportStage(transport),
            new AcknowledgmentStage(ackFunc)
        });

        var message = new Message(new byte[] { 1, 2, 3 });
        await pipeline.SendAsync(message);

        Debug.Assert(receiver.Received.ContainsKey(message.Id), "Message should be received.");
        Debug.Assert(receiver.Received[message.Id].Payload.Length == 3, "Payload length mismatch.");
    }

    static async Task TestRetryOnTransientFailure()
    {
        var transport = new InMemoryTransport();
        var receiver = new SimpleReceiver();
        transport.RegisterReceiver(receiver);

        int failCount = 0;
        Func<IMessage, Task<bool>> flakyAck = async msg =>
        {
            await Task.Yield();
            if (failCount < 2)
            {
                failCount++;
                return false; // simulate transient failure
            }
            return true;
        };

        var pipeline = new SendPipeline(new IMessageStage[]
        {
            new SerializationStage(),
            new TransportStage(transport),
            new RetryStage(new AcknowledgmentStage(flakyAck), maxAttempts: 5, delay: TimeSpan.FromMilliseconds(1))
        });

        var message = new Message(new byte[] { 9, 9, 9 });
        await pipeline.SendAsync(message);

        Debug.Assert(failCount == 2, "Exactly two transient failures expected.");
        Debug.Assert(receiver.Received.ContainsKey(message.Id), "Message should be received after retries.");
    }

    static async Task BenchmarkThroughput(int messageCount)
    {
        var transport = new InMemoryTransport();
        var receiver = new SimpleReceiver();
        transport.RegisterReceiver(receiver);

        Func<IMessage, Task<bool>> ack = async msg =>
        {
            await Task.Yield();
            return true;
        };

        var pipeline = new SendPipeline(new IMessageStage[]
        {
            new SerializationStage(),
            new TransportStage(transport),
            new AcknowledgmentStage(ack)
        });

        var sw = Stopwatch.StartNew();
        var tasks = new Task[messageCount];
        for (int i = 0; i < messageCount; i++)
        {
            var msg = new Message(BitConverter.GetBytes(i));
            tasks[i] = pipeline.SendAsync(msg);
        }
        await Task.WhenAll(tasks);
        sw.Stop();

        double msgsPerSec = messageCount / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Benchmark: Sent {messageCount} messages in {sw.Elapsed.TotalSeconds:F3}s ({msgsPerSec:F0} msgs/s)");
        Debug.Assert(receiver.Received.Count == messageCount, "All messages must be received.");
    }
}
