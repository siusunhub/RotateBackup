using RotateBackup.Core.Engine;
using Xunit;

namespace RotateBackup.Core.Tests
{
    public class SafetyValidatorTests
    {
        [Theory]
        [InlineData(@"C:\WINDOWS")]
        [InlineData(@"C:\Windows\System32")]
        [InlineData(@"C:\Program Files")]
        [InlineData(@"C:\Program Files (x86)")]
        [InlineData(@"C:\Users")]
        [InlineData(@"c:\users\test")]
        public void SafetyValidator_RejectsSystemPaths(string path)
        {
            var validator = new SafetyValidator();
            bool isSafe = validator.IsPathSafe(path, out string reason);
            Assert.False(isSafe);
            Assert.NotEmpty(reason);
        }

        [Theory]
        [InlineData(@"D:\Backups\Daily")]
        [InlineData(@"C:\MyData\Backup1")]
        [InlineData(@"E:\Files\Archive")]
        public void SafetyValidator_AllowsNormalPaths(string path)
        {
            var validator = new SafetyValidator();
            bool isSafe = validator.IsPathSafe(path, out string reason);
            Assert.True(isSafe);
            Assert.Empty(reason);
        }
    }
}
