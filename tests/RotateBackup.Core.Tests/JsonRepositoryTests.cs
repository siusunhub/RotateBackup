using System;
using System.Collections.Generic;
using System.IO;
using RotateBackup.Core.Models;
using RotateBackup.Core.Storage;
using Xunit;

namespace RotateBackup.Core.Tests
{
    public class JsonRepositoryTests
    {
        [Fact]
        public void JsonBackupRepository_SaveAndLoad_RoundTripsSuccessfully()
        {
            var repo = new JsonBackupRepository();
            var config = new BackupConfig
            {
                Version = "2.0",
                BackupSets = new List<BackupSet>
                {
                    new BackupSet
                    {
                        Id = "TEST1",
                        Command = "test_cmd",
                        Remark = "Test backup",
                        IsDirectory = true,
                        SourcePath = @"D:\Source",
                        Slots = new List<RotationSlot>
                        {
                            new RotationSlot { Index = 1, Path = @"D:\Backup1", Keep = false },
                            new RotationSlot { Index = 2, Path = @"D:\Backup2", Keep = true }
                        }
                    }
                }
            };

            string tempFile = Path.Combine(Path.GetTempPath(), $"test_backup_{Guid.NewGuid():N}.json");
            try
            {
                repo.Save(config, tempFile);
                var loaded = repo.Load(tempFile);

                Assert.NotNull(loaded);
                Assert.Single(loaded.BackupSets);
                Assert.Equal("test_cmd", loaded.BackupSets[0].Command);
                Assert.Equal(2, loaded.BackupSets[0].Slots.Count);
                Assert.True(loaded.BackupSets[0].Slots[1].Keep);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void JsonBackupRepository_Load_NonExistentFile_ReturnsEmptyConfig()
        {
            var repo = new JsonBackupRepository();
            string nonExistent = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json");

            var loaded = repo.Load(nonExistent);

            Assert.NotNull(loaded);
            Assert.NotNull(loaded.BackupSets);
            Assert.Empty(loaded.BackupSets);
        }
    }
}
