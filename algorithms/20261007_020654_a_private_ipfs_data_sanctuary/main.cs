using System.Collections.Generic;
using System;
using System.Text;

namespace PrivateIpfsSanctuary
{
    internal static class Program
    {
        private static void Main()
        {
            RunAllTests();
            Console.WriteLine("All tests passed.");
        }

        private static void RunAllTests()
        {
            TestAddAndRetrieve();
            TestDuplicateStorage();
            TestVerificationSuccess();
            TestVerificationFailure();
            TestMissingObject();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"Assertion failed: {message}");
        }

        private static void TestAddAndRetrieve()
        {
            var sanctuary = new Sanctuary();
            byte[] data = Encoding.UTF8.GetBytes("Hello, IPFS Sanctuary!");
            DigitalObject obj = sanctuary.Add(data);

            DigitalObject fetched = sanctuary.Get(obj.Hash);
            Assert(fetched != null, "Fetched object should not be null.");
            Assert(fetched.Id == obj.Id, "Fetched object ID should match.");
            Assert(Encoding.UTF8.GetString(fetched.Data) == "Hello, IPFS Sanctuary!", "Data round‑trip mismatch.");
        }

        private static void TestDuplicateStorage()
        {
            var sanctuary = new Sanctuary();
            byte[] data = Encoding.UTF8.GetBytes("duplicate");
            DigitalObject first = sanctuary.Add(data);
            DigitalObject second = sanctuary.Add(data);

            // Same hash expected.
            Assert(first.Hash == second.Hash, "Hashes of duplicate data must match.");
            // Store should contain only one entry for that hash.
            DigitalObject fetched = sanctuary.Get(first.Hash);
            Assert(fetched.Id == first.Id, "Original object should be retained.");
        }

        private static void TestVerificationSuccess()
        {
            var sanctuary = new Sanctuary();
            byte[] data = Encoding.UTF8.GetBytes("verify me");
            DigitalObject obj = sanctuary.Add(data);
            bool ok = sanctuary.Verify(obj.Hash);
            Assert(ok, "Verification should succeed for untouched data.");
        }

        private static void TestVerificationFailure()
        {
            var sanctuary = new Sanctuary();
            byte[] data = Encoding.UTF8.GetBytes("original");
            DigitalObject obj = sanctuary.Add(data);

            // Tamper with stored data via reflection (simulating corruption).
            var field = typeof(DigitalObject).GetProperty(nameof(DigitalObject.Data));
            // Since Data is read‑only, we cannot modify it directly; instead, we replace the entry in the store.
            // Re‑insert a corrupted object with same hash.
            var corrupted = new DigitalObject(obj.Id, Encoding.UTF8.GetBytes("corrupted"), obj.Hash);
            // Access private store via reflection.
            var storeField = typeof(Sanctuary).GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var store = (System.Collections.Concurrent.ConcurrentDictionary<string, DigitalObject>)storeField!.GetValue(sanctuary)!;
            store[obj.Hash] = corrupted;

            bool ok = sanctuary.Verify(obj.Hash);
            Assert(!ok, "Verification should fail for tampered data.");
        }

        private static void TestMissingObject()
        {
            var sanctuary = new Sanctuary();
            try
            {
                sanctuary.Get("nonexistenthash");
                Assert(false, "Expected exception for missing object.");
            }
            catch (InvalidOperationException)
            {
                // Expected.
            }
        }
    }
}
