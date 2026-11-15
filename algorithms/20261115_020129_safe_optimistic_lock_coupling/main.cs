using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OptimisticLockCoupling;

class Program
{
    static void Main()
    {
        TestSequentialOperations();
        TestConcurrentAdd();
        TestConcurrentRemove();
        Console.WriteLine("All tests passed.");
    }

    private static void TestSequentialOperations()
    {
        var list = new OptimisticList<int>();

        // Add
        Debug.Assert(list.Add(5));
        Debug.Assert(list.Add(3));
        Debug.Assert(list.Add(9));
        Debug.Assert(!list.Add(5)); // duplicate

        // Contains
        Debug.Assert(list.Contains(3));
        Debug.Assert(list.Contains(5));
        Debug.Assert(list.Contains(9));
        Debug.Assert(!list.Contains(7));

        // Remove
        Debug.Assert(list.Remove(5));
        Debug.Assert(!list.Contains(5));
        Debug.Assert(!list.Remove(5)); // already removed

        // Final order check
        var snapshot = list.ToList();
        Debug.Assert(snapshot.Count == 2);
        Debug.Assert(snapshot[0] == 3 && snapshot[1] == 9);
    }

    private static void TestConcurrentAdd()
    {
        const int threadCount = 8;
        const int perThread = 2500;
        var list = new OptimisticList<int>();
        var tasks = new Task[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int start = t * perThread;
            tasks[t] = Task.Run(() =>
            {
                for (int i = start; i < start + perThread; i++)
                {
                    bool added = list.Add(i);
                    Debug.Assert(added);
                }
            });
        }

        Task.WaitAll(tasks);

        // Verify all elements are present and sorted
        var snapshot = list.ToList();
        Debug.Assert(snapshot.Count == threadCount * perThread);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Debug.Assert(snapshot[i] == i);
        }
    }

    private static void TestConcurrentRemove()
    {
        const int total = 20000;
        var list = new OptimisticList<int>();
        for (int i = 0; i < total; i++)
            list.Add(i);

        const int threadCount = 10;
        const int perThread = total / threadCount;
        var tasks = new Task[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int start = t * perThread;
            int end = (t == threadCount - 1) ? total : start + perThread;
            tasks[t] = Task.Run(() =>
            {
                for (int i = start; i < end; i++)
                {
                    bool removed = list.Remove(i);
                    Debug.Assert(removed);
                }
            });
        }

        Task.WaitAll(tasks);

        // List should be empty
        var snapshot = list.ToList();
        Debug.Assert(snapshot.Count == 0);
    }
}
