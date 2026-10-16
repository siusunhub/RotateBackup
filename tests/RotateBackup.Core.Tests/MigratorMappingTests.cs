using RotateBackup.Migrator;
using Xunit;

namespace RotateBackup.Core.Tests
{
    public class MigratorMappingTests
    {
        [Fact]
        public void LegacyConverter_ConvertsSlotsAccurately()
        {
            var legacy = new LegacyBackupSetting
            {
                _id = "REC123",
                Command = "test_legacy",
                Remark = "Old backup",
                isDirectory = true,
                maxPath = 3,
                mainPath = @"D:\Source",
                Path1 = @"D:\Slot1",
                Path2 = @"D:\Slot2",
                Path3 = @"D:\Slot3",
                keepPath2 = true
            };

            var modern = LegacyConverter.ToModern(legacy);

            Assert.Equal("test_legacy", modern.Command);
            Assert.Equal("Old backup", modern.Remark);
            Assert.True(modern.IsDirectory);
            Assert.Equal(@"D:\Source", modern.SourcePath);
            Assert.Equal(3, modern.Slots.Count);
            Assert.Equal(@"D:\Slot1", modern.Slots[0].Path);
            Assert.False(modern.Slots[0].Keep);
            Assert.Equal(@"D:\Slot2", modern.Slots[1].Path);
            Assert.True(modern.Slots[1].Keep);
            Assert.Equal(@"D:\Slot3", modern.Slots[2].Path);
            Assert.False(modern.Slots[2].Keep);
        }
    }
}
