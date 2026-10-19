using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RotateBackup.Core.Engine;
using RotateBackup.Core.Models;
using Xunit;

namespace RotateBackup.Core.Tests
{
    public class BackupRunnerTests
    {
        [Fact]
        public async Task BackupRunner_DirectoryRotation_RollsFoldersCorrectly()
        {
            // Setup temp directory
            string testRoot = Path.Combine(Path.GetTempPath(), $"rotate_test_{Guid.NewGuid():N}");
            string sourceDir = Path.Combine(testRoot, "Source");
            string slot1Dir = Path.Combine(testRoot, "Slot1");
            string slot2Dir = Path.Combine(testRoot, "Slot2");

            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "source.txt"), "source content");

            Directory.CreateDirectory(slot1Dir);
            File.WriteAllText(Path.Combine(slot1Dir, "slot1.txt"), "slot1 content");

            var backupSet = new BackupSet
            {
                Command = "test_dir",
                IsDirectory = true,
                SourcePath = sourceDir,
                KeepSource = true,
                Slots = new List<RotationSlot>
                {
                    new RotationSlot { Index = 1, Path = slot1Dir, Keep = false },
                    new RotationSlot { Index = 2, Path = slot2Dir, Keep = false }
                }
            };

            var runner = new BackupRunner(new SafetyValidator(new[] { @"C:\WINDOWS" }));
            var logs = new List<string>();

            try
            {
                bool success = await runner.RunAsync(backupSet, logs.Add);

                Assert.True(success);

                // Slot 2 should now have the old slot 1 content
                Assert.True(Directory.Exists(slot2Dir));
                Assert.True(File.Exists(Path.Combine(slot2Dir, "slot1.txt")));

                // Slot 1 should have the source content
                Assert.True(Directory.Exists(slot1Dir));
                Assert.True(File.Exists(Path.Combine(slot1Dir, "source.txt")));

                // Because KeepSource = true, source should still exist
                Assert.True(Directory.Exists(sourceDir));
                Assert.True(File.Exists(Path.Combine(sourceDir, "source.txt")));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Fact]
        public async Task BackupRunner_FileRotation_RollsFilesCorrectly()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), $"rotate_file_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(testRoot);

            string sourceFile = Path.Combine(testRoot, "backup.zip");
            string slot1File = Path.Combine(testRoot, "backup.1.zip");
            string slot2File = Path.Combine(testRoot, "backup.2.zip");

            File.WriteAllText(sourceFile, "newest data");
            File.WriteAllText(slot1File, "older data");

            var backupSet = new BackupSet
            {
                Command = "test_file",
                IsDirectory = false,
                SourcePath = sourceFile,
                KeepSource = false,
                Slots = new List<RotationSlot>
                {
                    new RotationSlot { Index = 1, Path = slot1File, Keep = false },
                    new RotationSlot { Index = 2, Path = slot2File, Keep = false }
                }
            };

            var runner = new BackupRunner(new SafetyValidator(new[] { @"C:\WINDOWS" }));
            var logs = new List<string>();

            try
            {
                bool success = await runner.RunAsync(backupSet, logs.Add);

                Assert.True(success);
                Assert.True(File.Exists(slot2File));
                Assert.Equal("older data", File.ReadAllText(slot2File));

                Assert.True(File.Exists(slot1File));
                Assert.Equal("newest data", File.ReadAllText(slot1File));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Fact]
        public async Task BackupRunner_ActivityLogging_CreatesPerJobLogWithActivitiesAndTimestamps()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), $"rotate_log_test_{Guid.NewGuid():N}");
            string sourceDir = Path.Combine(testRoot, "Source");
            string slot1Dir = Path.Combine(testRoot, "Slot1");
            string logFile = Path.Combine(testRoot, "Logs", "my_backup_job.log");

            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "document.txt"), "Important file data");

            Directory.CreateDirectory(slot1Dir);

            var backupSet = new BackupSet
            {
                Command = "custom_job",
                Remark = "Testing Per-Job Logging",
                IsDirectory = true,
                SourcePath = sourceDir,
                KeepSource = true,
                EnableLog = true,
                LogPath = logFile,
                Slots = new List<RotationSlot>
                {
                    new RotationSlot { Index = 1, Path = slot1Dir, Keep = false }
                }
            };

            var runner = new BackupRunner(new SafetyValidator(new[] { @"C:\WINDOWS" }));
            var memoryLogs = new List<string>();

            try
            {
                bool success = await runner.RunAsync(backupSet, memoryLogs.Add);

                Assert.True(success);
                Assert.True(File.Exists(logFile), $"Log file was not created at expected location: {logFile}");

                string logContent = File.ReadAllText(logFile);

                // Verify activities in log file (first line, last line, summary, milestones)
                Assert.Contains("[START]", logContent);
                Assert.Contains("custom_job", logContent);
                Assert.Contains("[CONFIG]", logContent);
                Assert.Contains("[PROCESS DIR]", logContent);
                Assert.Contains("[COMPLETE]", logContent);

                // Verify that intermediate individual files are shown on screen (memoryLogs) but omitted from log file
                Assert.Contains(memoryLogs, l => l.Contains("[PROCESS FILE]") && l.Contains("document.txt"));
                Assert.DoesNotContain("document.txt", logContent);

                // Verify timestamp format [yyyy-MM-dd HH:mm:ss]
                string currentYear = DateTime.Now.Year.ToString();
                Assert.Contains($"[{currentYear}-", logContent);
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Fact]
        public async Task BackupRunner_LargeFile_CopyStreamAndVerifySuccess()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), $"large_file_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(testRoot);

            string sourceFile = Path.Combine(testRoot, "large_database.bak");
            string targetFile = Path.Combine(testRoot, "target_database.bak");

            // Generate an 8 MB test file
            byte[] chunk = new byte[1024 * 1024]; // 1MB
            Array.Fill<byte>(chunk, 0xAB);
            using (var fs = new FileStream(sourceFile, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < 8; i++)
                {
                    fs.Write(chunk, 0, chunk.Length);
                }
            }

            var logs = new List<string>();

            try
            {
                await BackupRunner.CopyFileWithRetryAndProgressAsync(sourceFile, targetFile, logs.Add, default);

                Assert.True(File.Exists(targetFile));
                Assert.Equal(new FileInfo(sourceFile).Length, new FileInfo(targetFile).Length);
                Assert.Contains(logs, l => l.Contains("[VERIFIED]"));
                Assert.Contains(logs, l => l.Contains("8.00 MB"));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Fact]
        public async Task BackupRunner_FileShareReadWrite_CopiesWhileSourceIsOpenByAnotherProcess()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), $"open_file_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(testRoot);

            string sourceFile = Path.Combine(testRoot, "active_log.db");
            string targetFile = Path.Combine(testRoot, "target_log.db");

            File.WriteAllText(sourceFile, "Active database data being written to...");

            var logs = new List<string>();

            // Simulate another process (e.g. database/service) holding an open handle with ReadWrite share
            using var activeHandle = new FileStream(sourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            try
            {
                // BackupRunner is able to read and copy with FileShare.ReadWrite even while activeHandle is open
                await BackupRunner.CopyFileWithRetryAndProgressAsync(sourceFile, targetFile, logs.Add, default);

                // Dispose active handle and verify target content matches
                activeHandle.Dispose();

                Assert.True(File.Exists(targetFile));
                Assert.Equal("Active database data being written to...", File.ReadAllText(targetFile));
            }
            finally
            {
                activeHandle.Dispose();
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Fact]
        public async Task BackupRunner_DirectoryRotation_UsesRenameStrategyAcrossSlots()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), $"rename_strat_{Guid.NewGuid():N}");
            string sourceDir = Path.Combine(testRoot, "Source");
            string slot1Dir = Path.Combine(testRoot, "1");
            string slot2Dir = Path.Combine(testRoot, "2");
            string slot3Dir = Path.Combine(testRoot, "3");

            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "source.txt"), "latest data");

            Directory.CreateDirectory(slot1Dir);
            File.WriteAllText(Path.Combine(slot1Dir, "slot1.txt"), "day 1 data");

            Directory.CreateDirectory(slot2Dir);
            File.WriteAllText(Path.Combine(slot2Dir, "slot2.txt"), "day 2 data");

            Directory.CreateDirectory(slot3Dir);
            File.WriteAllText(Path.Combine(slot3Dir, "slot3.txt"), "oldest data to be overwritten");

            var backupSet = new BackupSet
            {
                Command = "test_rename",
                IsDirectory = true,
                SourcePath = sourceDir,
                KeepSource = true,
                Slots = new List<RotationSlot>
                {
                    new RotationSlot { Index = 1, Path = slot1Dir, Keep = false },
                    new RotationSlot { Index = 2, Path = slot2Dir, Keep = false },
                    new RotationSlot { Index = 3, Path = slot3Dir, Keep = false }
                }
            };

            var runner = new BackupRunner(new SafetyValidator(new[] { @"C:\WINDOWS" }));
            var logs = new List<string>();

            try
            {
                bool success = await runner.RunAsync(backupSet, logs.Add);
                Assert.True(success);

                // Slot 3 should now have day 2 data (from old slot 2)
                Assert.True(Directory.Exists(slot3Dir));
                Assert.True(File.Exists(Path.Combine(slot3Dir, "slot2.txt")));

                // Slot 2 should now have day 1 data (from old slot 1)
                Assert.True(Directory.Exists(slot2Dir));
                Assert.True(File.Exists(Path.Combine(slot2Dir, "slot1.txt")));

                // Slot 1 should have latest data (from source)
                Assert.True(Directory.Exists(slot1Dir));
                Assert.True(File.Exists(Path.Combine(slot1Dir, "source.txt")));

                // Verify that [RENAME STRATEGY] was used for rolling slots
                Assert.Contains(logs, l => l.Contains("[RENAME STRATEGY]"));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }
    }
}
