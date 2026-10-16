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
    }
}
