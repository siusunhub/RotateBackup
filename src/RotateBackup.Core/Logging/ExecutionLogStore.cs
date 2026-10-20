using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RotateBackup.Core.Models;
using RotateBackup.Core.Validation;

namespace RotateBackup.Core.Logging
{
    /// <summary>
    /// Execution log service: records for every backup execution when it started, when it
    /// ended and whether it succeeded or failed. All executions of all jobs share ONE file
    /// (logs/execution.log).
    ///
    /// Multi-access design (the CLI, the WPF manager and Task Scheduler jobs may all write
    /// at the same time):
    ///   * Every append is serialized with a tiny cross-process lock file. The lock is held
    ///     for a few microseconds, so concurrent programs never corrupt or interleave lines
    ///     and never fail with "the file is being used by another process".
    ///   * Readers open the file with FileShare.ReadWrite | FileShare.Delete, so the log can
    ///     be inspected while another program is still appending to it.
    ///
    /// File layout (one line per event, human readable):
    ///   2026-10-20 15:30:12.345 | START | run=9f3a2b1c | pid=4821 | source=CLI | command=daily_sql
    ///   2026-10-20 15:31:45.678 | END | run=9f3a2b1c | pid=4821 | source=CLI | command=daily_sql | result=SUCCESS | duration=93.33s | message=
    /// </summary>
    public class ExecutionLogStore
    {
        /// <summary>Name of the single file that holds the execution log of every job.</summary>
        public const string ExecutionLogFileName = "execution.log";

        /// <summary>Default retention of execution log entries in days.</summary>
        public const int DefaultRetentionDays = 30;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly char[] FieldSeparator = { '|' };

        private readonly string _baseDirectory;

        /// <summary>
        /// Creates a store rooted at the given directory. The execution log is stored in
        /// &lt;baseDirectory&gt;/logs/execution.log.
        /// </summary>
        public ExecutionLogStore(string? baseDirectory)
        {
            _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : Path.GetFullPath(baseDirectory);
        }

        /// <summary>Folder that contains the execution log.</summary>
        public string LogDirectory => Path.Combine(_baseDirectory, "logs");

        /// <summary>Absolute path of the single execution log file.</summary>
        public string LogFilePath => Path.Combine(LogDirectory, ExecutionLogFileName);

        /// <summary>Path of the tiny lock file used to serialize appends between processes.</summary>
        public string LockFilePath => LogFilePath + ".lock";

        /// <summary>
        /// Registers the start of an execution and returns the created record.
        /// The START line is written immediately so other programs already show "RUNNING".
        /// </summary>
        public ExecutionRecord Start(string command, string source = "Unknown")
        {
            var runId = Guid.NewGuid().ToString("N")[..8];
            var now = DateTime.Now;

            var record = new ExecutionRecord
            {
                Command = string.IsNullOrWhiteSpace(command) ? "unknown" : command.Trim(),
                RunId = runId,
                StartedAt = now,
                Outcome = ExecutionOutcome.Running,
                ProcessId = Environment.ProcessId,
                Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim(),
                FilePath = LogFilePath
            };

            AppendLine(FormatStartLine(record));
            return record;
        }

        /// <summary>
        /// Closes an execution by appending the end marker (end time, duration and result).
        /// </summary>
        public bool Complete(ExecutionRecord record, bool success, string? message = null)
        {
            if (record == null) return false;

            try
            {
                DateTime endedAt = DateTime.Now;
                record.EndedAt = endedAt;
                record.DurationSeconds = Math.Max(0, (endedAt - record.StartedAt).TotalSeconds);
                record.Outcome = success ? ExecutionOutcome.Success : ExecutionOutcome.Failed;
                record.Message = message ?? string.Empty;

                AppendLine(FormatEndLine(record));
                return true;
            }
            catch
            {
                // Logging must never break the backup itself.
                return false;
            }
        }

        /// <summary>
        /// Returns the recorded executions, newest first.
        /// </summary>
        /// <param name="command">Only return executions of this command (null = every job).</param>
        /// <param name="maxCount">Maximum number of records returned (0 = unlimited).</param>
        public IReadOnlyList<ExecutionRecord> List(string? command = null, int maxCount = 200)
        {
            IEnumerable<ExecutionRecord> records = ParseAll();

            if (!string.IsNullOrWhiteSpace(command))
            {
                string wanted = command.Trim();
                records = records.Where(r => string.Equals(r.Command, wanted, StringComparison.OrdinalIgnoreCase));
            }

            return records
                .OrderByDescending(r => r.StartedAt)
                .ThenByDescending(r => r.RunId, StringComparer.Ordinal)
                .Take(maxCount <= 0 ? int.MaxValue : maxCount)
                .ToList();
        }

        /// <summary>Returns the most recent execution, or null when nothing was recorded yet.</summary>
        public ExecutionRecord? GetLatest(string? command = null)
        {
            return List(command, 1).FirstOrDefault();
        }

        /// <summary>Reads the raw lines that belong to a single execution.</summary>
        public string ReadDetail(ExecutionRecord record)
        {
            if (record == null) return string.Empty;

            List<string> lines = ReadLines()
                .Where(l => BelongsToRun(l, record.RunId))
                .ToList();

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Reads the log file. Safe to call while another program is appending to it.
        /// </summary>
        public IReadOnlyList<string> ReadLines()
        {
            if (!File.Exists(LogFilePath)) return Array.Empty<string>();

            try
            {
                using var stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => l.Trim().Length > 0)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Removes every execution that started more than <paramref name="keepDays"/> days ago.
        /// The rewrite happens while the lock is held, so no running program can append in
        /// between and lose data. Returns the number of removed lines.
        /// </summary>
        public int Cleanup(int keepDays = DefaultRetentionDays)
        {
            if (keepDays <= 0) return 0;

            DateTime cutoff = DateTime.Now.AddDays(-keepDays);
            int removed = 0;

            using (AcquireLock())
            {
                if (!File.Exists(LogFilePath)) return 0;

                List<string> lines = ReadLines().ToList();

                // Find the start time of every run so both lines of an expired run are dropped.
                var runStart = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                foreach (string line in lines)
                {
                    if (!TryParseLine(line, out string runId, out ExecutionRecord? parsed, out bool isEnd)) continue;
                    if (isEnd || parsed == null) continue;
                    runStart[runId] = parsed.StartedAt;
                }

                var kept = new List<string>(lines.Count);
                foreach (string line in lines)
                {
                    bool expired = false;

                    if (TryParseLine(line, out string runId, out ExecutionRecord? parsed, out _))
                    {
                        DateTime startedAt = runStart.TryGetValue(runId, out DateTime start) ? start : parsed!.StartedAt;
                        expired = startedAt < cutoff;
                    }

                    if (expired) removed++;
                    else kept.Add(line);
                }

                if (removed == 0) return 0;

                string tempPath = LogFilePath + ".tmp";
                try
                {
                    File.WriteAllLines(tempPath, kept, Utf8NoBom);
                    File.Move(tempPath, LogFilePath, overwrite: true);
                }
                catch
                {
                    // Fallback: rewrite in place (still protected by the lock).
                    try
                    {
                        File.WriteAllLines(LogFilePath, kept, Utf8NoBom);
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }

            return removed;
        }

        /// <summary>Parses the whole log file into execution records.</summary>
        public IReadOnlyList<ExecutionRecord> ParseAll()
        {
            var records = new Dictionary<string, ExecutionRecord>(StringComparer.Ordinal);
            var order = new List<string>();

            foreach (string line in ReadLines())
            {
                if (!TryParseLine(line, out string runId, out ExecutionRecord? parsed, out bool isEnd)) continue;
                if (parsed == null) continue;

                if (!records.TryGetValue(runId, out ExecutionRecord? existing))
                {
                    records[runId] = parsed;
                    order.Add(runId);
                }
                else if (isEnd)
                {
                    existing.EndedAt = parsed.EndedAt;
                    existing.DurationSeconds = parsed.DurationSeconds;
                    existing.Outcome = parsed.Outcome;
                    existing.Message = parsed.Message;
                }
            }

            return order.Select(id => records[id]).ToList();
        }

        /// <summary>Parses a single log line. Returns false for lines that are not log entries.</summary>
        public static bool TryParseLine(string rawLine, out string runId, out ExecutionRecord? record, out bool isEnd)
        {
            runId = string.Empty;
            record = null;
            isEnd = false;

            string line = rawLine.Trim();
            if (line.Length == 0) return false;

            string[] parts = line.Split(FieldSeparator, StringSplitOptions.TrimEntries);
            if (parts.Length < 3) return false;

            string marker = parts[1].ToUpperInvariant();
            if (marker != "START" && marker != "END") return false;

            Dictionary<string, string> values = ParseKeyValues(parts, 2);
            runId = GetValue(values, "run", string.Empty);
            if (runId.Length == 0) return false;

            DateTime timestamp = ParseTimestamp(parts[0]);
            var parsed = new ExecutionRecord
            {
                RunId = runId,
                Command = GetValue(values, "command", string.Empty),
                Source = GetValue(values, "source", string.Empty),
                ProcessId = int.TryParse(GetValue(values, "pid", "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid) ? pid : 0
            };

            if (marker == "START")
            {
                parsed.StartedAt = timestamp;
            }
            else
            {
                isEnd = true;
                parsed.EndedAt = timestamp;
                parsed.Outcome = string.Equals(GetValue(values, "result", string.Empty), "SUCCESS", StringComparison.OrdinalIgnoreCase)
                    ? ExecutionOutcome.Success
                    : ExecutionOutcome.Failed;

                string duration = GetValue(values, "duration", string.Empty);
                if (double.TryParse(duration.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    parsed.DurationSeconds = seconds;
                }

                parsed.Message = GetValue(values, "message", string.Empty);
            }

            record = parsed;
            return true;
        }

        /// <summary>Checks whether a log line belongs to the given execution.</summary>
        public static bool BelongsToRun(string rawLine, string runId)
        {
            if (!TryParseLine(rawLine, out string lineRunId, out _, out _)) return false;
            return string.Equals(lineRunId, runId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Appends a line to the log file, serialized across all processes.</summary>
        private void AppendLine(string line)
        {
            Directory.CreateDirectory(LogDirectory);

            using (AcquireLock())
            {
                using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using var writer = new StreamWriter(stream, Utf8NoBom);
                writer.WriteLine(line);
                writer.Flush();
            }
        }

        /// <summary>
        /// Acquires the cross-process lock. A dedicated lock file is used (instead of a named
        /// mutex) because it works across Windows sessions and does not need any privilege.
        /// </summary>
        private FileStream AcquireLock()
        {
            for (int attempt = 1; attempt <= 200; attempt++)
            {
                try
                {
                    return new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    Thread.Sleep(25);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(25);
                }
            }

            throw new IOException($"Timed out while waiting for the execution log lock '{LockFilePath}'.");
        }

        private static string FormatStartLine(ExecutionRecord record)
        {
            return $"{record.StartedAt:yyyy-MM-dd HH:mm:ss.fff} | START | run={record.RunId} | pid={record.ProcessId} | source={CommandNameValidator.SanitizeForLog(record.Source)} | command={CommandNameValidator.SanitizeForLog(record.Command)}";
        }

        private static string FormatEndLine(ExecutionRecord record)
        {
            string duration = record.DurationSeconds.HasValue
                ? record.DurationSeconds.Value.ToString("F2", CultureInfo.InvariantCulture)
                : "0.00";

            string result = record.Outcome == ExecutionOutcome.Success ? "SUCCESS" : "FAILED";

            return $"{record.EndedAt:yyyy-MM-dd HH:mm:ss.fff} | END | run={record.RunId} | pid={record.ProcessId} | source={CommandNameValidator.SanitizeForLog(record.Source)} | command={CommandNameValidator.SanitizeForLog(record.Command)} | result={result} | duration={duration}s | message={CommandNameValidator.SanitizeForLog(record.Message)}";
        }

        private static DateTime ParseTimestamp(string value)
        {
            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
            {
                return exact;
            }

            if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime loose))
            {
                return loose;
            }

            return default;
        }

        private static Dictionary<string, string> ParseKeyValues(string[] parts, int startIndex)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = startIndex; i < parts.Length; i++)
            {
                string part = parts[i];
                int separator = part.IndexOf('=');
                if (separator <= 0) continue;

                values[part.Substring(0, separator).Trim()] = part.Substring(separator + 1).Trim();
            }

            return values;
        }

        private static string GetValue(Dictionary<string, string> values, string key, string fallback)
        {
            return values.TryGetValue(key, out string? value) ? value : fallback;
        }
    }
}
