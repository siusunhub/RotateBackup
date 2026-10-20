using System;

namespace RotateBackup.Core.Models
{
    /// <summary>
    /// Result of a single backup execution.
    /// </summary>
    public enum ExecutionOutcome
    {
        /// <summary>Execution is still running (no end marker written yet).</summary>
        Running,

        /// <summary>Execution finished successfully.</summary>
        Success,

        /// <summary>Execution finished with an error / failure.</summary>
        Failed
    }

    /// <summary>
    /// Lightweight description of ONE backup execution: when it started, when it ended
    /// and whether it succeeded or failed.
    ///
    /// Every execution owns its own file on disk (see ExecutionLogStore), so several
    /// programs may run and log at the same time without sharing a single file handle.
    /// </summary>
    public class ExecutionRecord
    {
        /// <summary>Command name of the backup set that was executed.</summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>Short unique id of this execution.</summary>
        public string RunId { get; set; } = string.Empty;

        /// <summary>Timestamp of when the execution started.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Timestamp of when the execution ended (null while still running).</summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>Total duration in seconds (null while still running).</summary>
        public double? DurationSeconds { get; set; }

        /// <summary>Outcome of the execution.</summary>
        public ExecutionOutcome Outcome { get; set; } = ExecutionOutcome.Running;

        /// <summary>Process id of the program that performed the execution.</summary>
        public int ProcessId { get; set; }

        /// <summary>Which program performed the execution (CLI, Manager, ...).</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Failure reason (or extra information) recorded at the end.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Absolute path of the file that stores this record.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>True while the execution has not reported an end yet.</summary>
        public bool IsRunning => Outcome == ExecutionOutcome.Running;
    }
}
