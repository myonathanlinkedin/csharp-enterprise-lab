using System;
using System.Collections.Generic;

namespace RustDeriveDemo
{
    /// <summary>
    /// Represents a field within a Rust struct.
    /// </summary>
    public sealed class RustField
    {
        public string Name { get; }
        public string Type { get; }

        public RustField(string name, string type)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Field name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Field type cannot be empty.", nameof(type));

            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// Enumerates common Rust derive traits that affect inlineability.
    /// </summary>
    [Flags]
    public enum DeriveTrait
    {
        None = 0,
        Debug = 1 << 0,
        Clone = 1 << 1,
        Copy = 1 << 2,
        PartialEq = 1 << 3,
        Eq = 1 << 4,
        PartialOrd = 1 << 5,
        Ord = 1 << 6,
        Hash = 1 << 7,
        Default = 1 << 8,
        // Extend as needed.
    }

    /// <summary>
    /// Represents a Rust struct definition with optional derive attributes.
    /// </summary>
    public sealed class RustStruct
    {
        public string Name { get; }
        public IReadOnlyList<RustField> Fields { get; }
        public DeriveTrait Derives { get; }

        public RustStruct(string name, IEnumerable<RustField> fields, DeriveTrait derives = DeriveTrait.None)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Struct name cannot be empty.", nameof(name));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            Name = name;
            Fields = new List<RustField>(fields).AsReadOnly();
            Derives = derives;
        }
    }
}
