using System;
using System.Collections.Generic;

namespace LamportSync
{
    /// <summary>
    /// Core engine that manages a collection of processes and provides Lamport‑clock operations.
    /// </summary>
    public sealed class LamportEngine
    {
        private readonly Dictionary<string, Process> _processes = new();

        /// <summary>
        /// Registers a new process with the given identifier.
        /// </summary>
        public void RegisterProcess(string processId)
        {
            if (processId == null) throw new ArgumentNullException(nameof(processId));
            if (_processes.ContainsKey(processId))
                throw new InvalidOperationException($"Process '{processId}' is already registered.");

            _processes[processId] = new Process(processId);
        }

        /// <summary>
        /// Retrieves the current timestamp of the specified process without side effects.
        /// </summary>
        public Timestamp GetCurrent(string processId)
        {
            var proc = GetProcess(processId);
            return proc.Current;
        }

        /// <summary>
        /// Records an internal event for the given process.
        /// </summary>
        public Timestamp InternalEvent(string processId)
        {
            var proc = GetProcess(processId);
            return proc.Tick();
        }

        /// <summary>
        /// Records a send event for the given process.
        /// The timestamp returned is the one attached to the outgoing message.
        /// </summary>
        public Timestamp SendEvent(string processId)
        {
            var proc = GetProcess(processId);
            return proc.Tick();
        }

        /// <summary>
        /// Records a receive event for the given process using the timestamp attached to the incoming message.
        /// </summary>
        public Timestamp ReceiveEvent(string processId, Timestamp incomingTimestamp)
        {
            var proc = GetProcess(processId);
            return proc.UpdateOnReceive(incomingTimestamp);
        }

        private Process GetProcess(string processId)
        {
            if (processId == null) throw new ArgumentNullException(nameof(processId));
            if (!_processes.TryGetValue(processId, out var proc))
                throw new InvalidOperationException($"Process '{processId}' is not registered.");
            return proc;
        }
    }
}
