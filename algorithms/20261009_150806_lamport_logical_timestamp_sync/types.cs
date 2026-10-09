using System;

namespace LamportSync
{
    /// <summary>
    /// Immutable Lamport timestamp.
    /// </summary>
    public readonly struct Timestamp : IComparable<Timestamp>, IEquatable<Timestamp>
    {
        public long Value { get; }

        public Timestamp(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be non‑negative.");
            Value = value;
        }

        public int CompareTo(Timestamp other) => Value.CompareTo(other.Value);
        public bool Equals(Timestamp other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is Timestamp other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"T[{Value}]";

        public static bool operator <(Timestamp left, Timestamp right) => left.Value < right.Value;
        public static bool operator >(Timestamp left, Timestamp right) => left.Value > right.Value;
        public static bool operator <=(Timestamp left, Timestamp right) => left.Value <= right.Value;
        public static bool operator >=(Timestamp left, Timestamp right) => left.Value >= right.Value;
        public static bool operator ==(Timestamp left, Timestamp right) => left.Equals(right);
        public static bool operator !=(Timestamp left, Timestamp right) => !left.Equals(right);
    }

    /// <summary>
    /// Represents a logical process participating in Lamport synchronization.
    /// </summary>
    public sealed class Process
    {
        public string Id { get; }

        // Backing field for the logical clock; must be accessed via Interlocked if concurrency is added.
        private long _clock;

        public Process(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _clock = 0;
        }

        /// <summary>
        /// Returns the current timestamp without mutating the clock.
        /// </summary>
        public Timestamp Current => new Timestamp(_clock);

        /// <summary>
        /// Increments the logical clock and returns the new timestamp.
        /// </summary>
        public Timestamp Tick()
        {
            _clock++;
            return new Timestamp(_clock);
        }

        /// <summary>
        /// Updates the clock according to a received timestamp.
        /// Implements: C = max(C, received) + 1
        /// </summary>
        public Timestamp UpdateOnReceive(Timestamp received)
        {
            _clock = Math.Max(_clock, received.Value) + 1;
            return new Timestamp(_clock);
        }
    }
}
