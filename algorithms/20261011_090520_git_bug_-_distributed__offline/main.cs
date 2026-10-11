using System;
using System.Diagnostics;
using GitBugTracker;

namespace GitBugTrackerTest
{
    internal static class Program
    {
        private static void Main()
        {
            TestAddAndRetrieve();
            TestUpdate();
            TestMergeDominance();
            TestMergeConcurrentResolution();
            Console.WriteLine("All tests passed.");
        }

        private static void TestAddAndRetrieve()
        {
            var tracker = new BugTracker("A");
            var id = tracker.AddBug("Crash on launch", "App crashes when started.", BugStatus.Open);
            var bug = tracker.GetBug(id);
            Debug.Assert(bug != null, "Bug should be retrieved.");
            Debug.Assert(bug.Title == "Crash on launch");
            Debug.Assert(bug.Status == BugStatus.Open);
        }

        private static void TestUpdate()
        {
            var tracker = new BugTracker("A");
            var id = tracker.AddBug("UI glitch", "Button misaligned.", BugStatus.Open);
            bool ok = tracker.UpdateBug(id, status: BugStatus.InProgress);
            Debug.Assert(ok, "Update should succeed.");
            var bug = tracker.GetBug(id);
            Debug.Assert(bug != null && bug.Status == BugStatus.InProgress);
        }

        private static void TestMergeDominance()
        {
            var a = new BugTracker("A");
            var b = new BugTracker("B");

            var id = a.AddBug("Memory leak", "Leak after heavy use.", BugStatus.Open);
            // Sync to B
            b.Merge(a);
            // Update in A
            a.UpdateBug(id, status: BugStatus.InProgress);
            // Merge back to B – A dominates
            b.Merge(a);
            var bugB = b.GetBug(id);
            Debug.Assert(bugB != null && bugB.Status == BugStatus.InProgress);
        }

        private static void TestMergeConcurrentResolution()
        {
            var a = new BugTracker("A");
            var b = new BugTracker("B");

            var id = a.AddBug("Sync issue", "Data not syncing.", BugStatus.Open);
            // Sync initial state
            b.Merge(a);

            // Concurrent updates
            a.UpdateBug(id, status: BugStatus.InProgress);
            System.Threading.Thread.Sleep(10); // ensure timestamp difference
            b.UpdateBug(id, status: BugStatus.Resolved);

            // Merge A into B
            b.Merge(a);
            var bug = b.GetBug(id);
            Debug.Assert(bug != null);
            // The later timestamp (B's update) should win
            Debug.Assert(bug.Status == BugStatus.Resolved);
        }
    }
}
