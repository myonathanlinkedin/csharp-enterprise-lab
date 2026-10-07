using System;

namespace PrivateIpfsSanctuary
{
    // Represents a digital object stored in the sanctuary.
    public sealed class DigitalObject
    {
        public Guid Id { get; }
        public byte[] Data { get; }
        public string Hash { get; }

        public DigitalObject(Guid id, byte[] data, string hash)
        {
            Id = id;
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }
    }

    // Interface for a content‑addressable storage system.
    public interface IContentAddressableStorage
    {
        // Stores the data and returns the corresponding DigitalObject.
        DigitalObject Add(byte[] data);

        // Retrieves a stored object by its hash. Throws if not found.
        DigitalObject Get(string hash);

        // Verifies that the stored object's hash matches its data.
        bool Verify(string hash);
    }
}
