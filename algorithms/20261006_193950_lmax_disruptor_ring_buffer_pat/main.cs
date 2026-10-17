using System;
using System.Threading;
using System.Threading.Tasks;
using DisruptorDemo;

namespace DisruptorDemo
{
    class Program
    {
        static void Main()
        {
            RunTests();
        }

        static void RunTests()
        {
            TestSingleProducerSingleConsumer();
            TestBlockingBehavior();
            Console.WriteLine("All tests passed.");
        }

        static void TestSingleProducerSingleConsumer()
        {
            const int count = 1000;
            var buffer = new RingBuffer<int>(8);
            var produced = new int[count];
            var consumed = new int[count];
            var consumerTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var seq = i;
                    var value = buffer.Get(seq);
                    consumed[i] = value;
                }
            });

            for (int i = 0; i < count; i++)
            {
                var seq = buffer.Next();
                produced[i] = i;
                buffer.Publish(seq, i);
            }

            consumerTask.Wait();

            for (int i = 0; i < count; i++)
            {
                if (produced[i] != consumed[i])
                    throw new Exception($"Mismatch at index {i}: produced {produced[i]}, consumed {consumed[i]}");
            }
        }

        static void TestBlockingBehavior()
        {
            var buffer = new RingBuffer<string>(4);
            string result = null;
            var consumerTask = Task.Run(() =>
            {
                var seq = 0L;
                result = buffer.Get(seq);
            });

            Thread.Sleep(50); // ensure consumer is waiting
            var seqPub = buffer.Next();
            buffer.Publish(seqPub, "hello");

            consumerTask.Wait();

            if (result != "hello")
                throw new Exception($"Blocking test failed: expected 'hello', got '{result}'");
        }
    }
}
