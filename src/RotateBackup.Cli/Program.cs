using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RotateBackup.Core.Engine;
using RotateBackup.Core.Models;
using RotateBackup.Core.Storage;

namespace RotateBackup.Cli
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" RotateBackup CLI Runner (.NET 10)");
            Console.WriteLine("========================================");

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
            {
                Console.WriteLine("Usage: RotateBackup.exe <command_name>");
                Console.WriteLine("Example: RotateBackup.exe daily_sql");
                return 1;
            }

            string commandName = args[0].Trim();
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(appDir, "backup_settings.json");

            if (!File.Exists(configPath))
            {
                // Also check current directory if different
                string currentDirConfig = Path.Combine(Environment.CurrentDirectory, "backup_settings.json");
                if (File.Exists(currentDirConfig))
                {
                    configPath = currentDirConfig;
                }
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

            var runner = new BackupRunner();
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
    }
}
