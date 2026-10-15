using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PhobosDemo
{
    internal static class Program
    {
        private static void Main()
        {
            RunAllTests();
            Console.WriteLine("All Phobos CircularBuffer tests passed.");
        }

        private static void RunAllTests()
        {
            TestConstruction();
            TestEnqueueDequeueOrder();
            TestWrapAroundBehavior();
            TestFullBufferException();
            TestEmptyBufferException();
            TestPeek();
            TestEnumeration();
        }

        private static void TestConstruction()
        {
            var buf = new CircularBuffer<int>(5);
            Debug.Assert(buf.Count == 0);
            Debug.Assert(buf.Capacity == 5);
            Debug.Assert(buf.IsEmpty);
            Debug.Assert(!buf.IsFull);
        }

        private static void TestEnqueueDequeueOrder()
        {
            var buf = new CircularBuffer<string>(3);
            buf.Enqueue("A");
            buf.Enqueue("B");
            buf.Enqueue("C");
            Debug.Assert(buf.IsFull);
            Debug.Assert(buf.Dequeue() == "A");
            Debug.Assert(buf.Dequeue() == "B");
            Debug.Assert(buf.Dequeue() == "C");
            Debug.Assert(buf.IsEmpty);
        }

        private static void TestWrapAroundBehavior()
        {
            var buf = new CircularBuffer<int>(4);
            // Fill partially
            buf.Enqueue(1);
            buf.Enqueue(2);
            buf.Enqueue(3);
            Debug.Assert(buf.Dequeue() == 1);
            Debug.Assert(buf.Dequeue() == 2);
            // At this point head=2, tail=3, count=1 (element 3)
            buf.Enqueue(4);
            buf.Enqueue(5); // This should wrap tail to index 0
            Debug.Assert(buf.Count == 3);
            Debug.Assert(buf.Dequeue() == 3);
            Debug.Assert(buf.Dequeue() == 4);
            Debug.Assert(buf.Dequeue() == 5);
            Debug.Assert(buf.IsEmpty);
        }

        private static void TestFullBufferException()
        {
            var buf = new CircularBuffer<int>(2);
            buf.Enqueue(10);
            buf.Enqueue(20);
            bool threw = false;
            try { buf.Enqueue(30); } catch (InvalidOperationException) { threw = true; }
            Debug.Assert(threw, "Enqueue on full buffer should throw.");
        }

        private static void TestEmptyBufferException()
        {
            var buf = new CircularBuffer<int>(2);
            bool threwDequeue = false;
            bool threwPeek = false;
            try { buf.Dequeue(); } catch (InvalidOperationException) { threwDequeue = true; }
            try { buf.Peek(); } catch (InvalidOperationException) { threwPeek = true; }
            Debug.Assert(threwDequeue, "Dequeue on empty buffer should throw.");
            Debug.Assert(threwPeek, "Peek on empty buffer should throw.");
        }

        private static void TestPeek()
        {
            var buf = new CircularBuffer<char>(3);
            buf.Enqueue('x');
            buf.Enqueue('y');
            Debug.Assert(buf.Peek() == 'x');
            Debug.Assert(buf.Count == 2);
            Debug.Assert(buf.Dequeue() == 'x');
            Debug.Assert(buf.Peek() == 'y');
        }

        private static void TestEnumeration()
        {
            var buf = new CircularBuffer<int>(5);
            var expected = new List<int> { 7, 8, 9 };
            foreach (var v in expected) buf.Enqueue(v);
            int idx = 0;
            foreach (var item in buf)
            {
                Debug.Assert(item == expected[idx], $"Enumeration mismatch at index {idx}");
                idx++;
            }
            Debug.Assert(idx == expected.Count);
        }
    }
}
