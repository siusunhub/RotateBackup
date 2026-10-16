using System;
using System.IO;
using System.Linq;
using LiteDB;
using RotateBackup.Core.Models;
using RotateBackup.Core.Storage;

namespace RotateBackup.Migrator
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine(" RotateBackup Legacy Migrator (LiteDB -> JSON)");
            Console.WriteLine("==============================================");

            string dbPath = args.Length > 0 ? args[0] : "BackupSet.db";
            string outputPath = args.Length > 1 ? args[1] : "backup_settings.json";

            if (!File.Exists(dbPath))
            {
                // Try parent directories or base directory
                string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
                if (File.Exists(fallback))
                {
                    dbPath = fallback;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Could not find legacy database at: {dbPath}");
                    Console.WriteLine("Usage: RotateBackup.Migrator.exe [path_to_BackupSet.db] [output_json_path]");
                    Console.ResetColor();
                    return 1;
                }
            }

            Console.WriteLine($"Reading legacy LiteDB database from: {dbPath}");

            var config = new BackupConfig { Version = "2.0" };

            try
            {
                using (var db = new LiteDatabase(dbPath))
                {
                    var col = db.GetCollection<LegacyBackupSetting>("backup");
                    var items = col.FindAll().ToList();

                    Console.WriteLine($"Found {items.Count} backup set(s) in legacy database.");

                    foreach (var legacy in items)
                    {
                        var modern = LegacyConverter.ToModern(legacy);
                        config.BackupSets.Add(modern);
                        Console.WriteLine($"  - Converted: '{modern.Command}' ({modern.Remark}) with {modern.Slots.Count} slots");
                    }
                }

                var repo = new JsonBackupRepository();
                repo.Save(config, outputPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCCESS] Successfully migrated {config.BackupSets.Count} backup set(s) to: {outputPath}");
                Console.ResetColor();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Migration failed: {ex.Message}");
                Console.ResetColor();
                return 2;
            }
        }
    }
}
