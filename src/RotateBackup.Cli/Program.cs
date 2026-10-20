using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RotateBackup.Core.Engine;
using RotateBackup.Core.Logging;
using RotateBackup.Core.Models;
using RotateBackup.Core.Storage;
using RotateBackup.Core.Validation;

namespace RotateBackup.Cli
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" RotateBackup CLI Runner v0.2 (.NET 10)");
            Console.WriteLine("========================================");

            if (args.Length == 0 || IsHelp(args[0]))
            {
                PrintHelp();
                return 1;
            }

            string configPath = ResolveConfigPath();

            // Execution log mode: shows when each backup started / ended and its result.
            if (string.Equals(args[0], "logs", StringComparison.OrdinalIgnoreCase))
            {
                return RunLogsCommand(configPath, args.Skip(1).ToArray());
            }

            return await RunBackupCommand(configPath, args[0]);
        }

        private static bool IsHelp(string arg)
        {
            return arg == "-h" || arg == "--help" || arg == "/?" || arg == "/help";
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  RotateBackup.exe <command_name>            Run the backup set with the given command name");
            Console.WriteLine("  RotateBackup.exe logs [command_name]       Show the execution log (start time, end time, result)");
            Console.WriteLine();
            Console.WriteLine("Execution log options:");
            Console.WriteLine("  --limit <n>     Maximum number of executions to list (default: 25)");
            Console.WriteLine("  --latest        Print the details of the most recent execution instead of the list");
            Console.WriteLine("  --cleanup [n]   Remove executions older than n days (default: 30) and exit");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RotateBackup.exe daily_sql");
            Console.WriteLine("  RotateBackup.exe logs");
            Console.WriteLine("  RotateBackup.exe logs daily_sql --limit 10");
            Console.WriteLine("  RotateBackup.exe logs daily_sql --latest");
            Console.WriteLine("  RotateBackup.exe logs --cleanup 30");
        }

        /// <summary>
        /// Configuration is looked up next to the executable first, then in the current directory.
        /// The execution log is stored in the same folder as the configuration file.
        /// </summary>
        private static string ResolveConfigPath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(appDir, "backup_settings.json");

            if (!File.Exists(configPath))
            {
                string currentDirConfig = Path.Combine(Environment.CurrentDirectory, "backup_settings.json");
                if (File.Exists(currentDirConfig))
                {
                    configPath = currentDirConfig;
                }
            }

            return configPath;
        }

        private static async Task<int> RunBackupCommand(string configPath, string commandName)
        {
            commandName = commandName.Trim();

            // The command name is used as a CLI argument, so it may only contain simple,
            // readable characters.
            if (!CommandNameValidator.IsValid(commandName, out string commandError))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {commandError}");
                Console.WriteLine($"        Allowed: {CommandNameValidator.AllowedCharacters}");
                Console.ResetColor();
                return 2;
            }

            Console.WriteLine($"Loading configuration from: {configPath}");
            var repo = new JsonBackupRepository();
            var config = repo.Load(configPath);

            var backupSet = config.BackupSets.FirstOrDefault(
                s => string.Equals(s.Command, commandName, StringComparison.OrdinalIgnoreCase));

            if (backupSet == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Backup set with command '{commandName}' not found in configuration.");
                Console.ResetColor();
                return 2;
            }

            if (backupSet.Disabled)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] Backup set '{commandName}' is currently marked as DISABLED. Skipping execution.");
                Console.ResetColor();
                return 2;
            }

            var runner = new BackupRunner(
                executionLogStore: new ExecutionLogStore(Path.GetDirectoryName(configPath)),
                source: "CLI");

            bool result = await runner.RunAsync(backupSet, log => Console.WriteLine(log));

            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCCESS] Backup '{commandName}' completed successfully.");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAILED] Backup '{commandName}' execution failed.");
                Console.ResetColor();
                return 3;
            }
        }

        /// <summary>
        /// Prints the execution log: for every execution the start time, the end time and
        /// whether it finished successfully or with an error.
        /// </summary>
        private static int RunLogsCommand(string configPath, string[] args)
        {
            string? command = null;
            int limit = 25;
            bool showLatest = false;
            int cleanupDays = 0;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.StartsWith("--limit", StringComparison.OrdinalIgnoreCase))
                {
                    string value = ReadOptionValue(arg, args, ref i);
                    limit = int.TryParse(value, out int parsed) && parsed > 0 ? parsed : 25;
                }
                else if (arg.StartsWith("--cleanup", StringComparison.OrdinalIgnoreCase))
                {
                    string value = ReadOptionValue(arg, args, ref i);
                    cleanupDays = int.TryParse(value, out int parsed) && parsed > 0
                        ? parsed
                        : ExecutionLogStore.DefaultRetentionDays;
                }
                else if (arg.StartsWith("--latest", StringComparison.OrdinalIgnoreCase))
                {
                    showLatest = true;
                }
                else if (arg.StartsWith("-", StringComparison.Ordinal))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Unknown option '{arg}'.");
                    Console.ResetColor();
                    return 1;
                }
                else if (command == null)
                {
                    command = arg;
                }
            }

            string baseDirectory = Path.GetDirectoryName(configPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var store = new ExecutionLogStore(baseDirectory);

            if (cleanupDays > 0)
            {
                int removed = store.Cleanup(cleanupDays);
                Console.WriteLine();
                Console.WriteLine($"Removed {removed} execution log entr{(removed == 1 ? "y" : "ies")} older than {cleanupDays} days.");
                Console.WriteLine($"Execution log file: {store.LogFilePath}");
                return 0;
            }

            IReadOnlyList<ExecutionRecord> records = store.List(command, limit);

            if (showLatest)
            {
                if (records.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[INFO] No execution history found for '{command ?? "any job"}'.");
                    Console.ResetColor();
                    return 1;
                }

                ExecutionRecord latest = records[0];
                Console.WriteLine();
                Console.WriteLine($"Latest execution of '{latest.Command}' (run {latest.RunId}, pid {latest.ProcessId}, source {latest.Source}):");
                Console.WriteLine(store.ReadDetail(latest));
                return 0;
            }

            Console.WriteLine();
            Console.WriteLine($"Execution log file: {store.LogFilePath}");

            if (records.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[INFO] No execution history found for '{command ?? "any job"}'.");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine(Pad("Started", 21) + Pad("Ended", 21) + Pad("Duration", 11) + Pad("Result", 10) + Pad("Command", 22) + "Source");
            Console.WriteLine(new string('-', 96));

            foreach (ExecutionRecord record in records)
            {
                string started = record.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
                string ended = record.EndedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
                string duration = record.DurationSeconds.HasValue ? $"{record.DurationSeconds.Value:F2}s" : "-";
                string result = record.Outcome switch
                {
                    ExecutionOutcome.Success => "SUCCESS",
                    ExecutionOutcome.Failed => "FAILED",
                    _ => "RUNNING"
                };

                Console.Write(Pad(started, 21) + Pad(ended, 21) + Pad(duration, 11));

                Console.ForegroundColor = record.Outcome switch
                {
                    ExecutionOutcome.Success => ConsoleColor.Green,
                    ExecutionOutcome.Failed => ConsoleColor.Red,
                    _ => ConsoleColor.Cyan
                };
                Console.Write(Pad(result, 10));
                Console.ResetColor();

                Console.WriteLine(Pad(record.Command, 22) + record.Source);
            }

            Console.WriteLine();
            Console.WriteLine($"{records.Count} execution(s) shown.");

            return 0;
        }

        private static string ReadOptionValue(string arg, string[] args, ref int index)
        {
            // Supports both "--limit 10" and "--limit=10"
            int separator = arg.IndexOf('=');
            if (separator > 0 && separator < arg.Length - 1)
            {
                return arg.Substring(separator + 1);
            }

            // Only consume the next argument when it is a number, so a command name
            // (e.g. "logs --cleanup job_one") is never swallowed by an option.
            if (index + 1 < args.Length &&
                !args[index + 1].StartsWith("-", StringComparison.Ordinal) &&
                int.TryParse(args[index + 1], out _))
            {
                index++;
                return args[index];
            }

            return string.Empty;
        }

        private static string Pad(string value, int width)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            if (value.Length >= width) return value.Substring(0, width - 1) + " ";
            return value.PadRight(width);
        }
    }
}
