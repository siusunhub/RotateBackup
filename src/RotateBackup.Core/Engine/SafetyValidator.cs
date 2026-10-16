using System;
using System.IO;

namespace RotateBackup.Core.Engine
{
    public class SafetyValidator
    {
        private static readonly string[] DefaultProtectedPaths =
        {
            @"C:\WINDOWS",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\Users"
        };

        private readonly string[] _protectedPaths;

        public SafetyValidator(string[]? customProtectedPaths = null)
        {
            _protectedPaths = customProtectedPaths ?? DefaultProtectedPaths;
        }

        public bool IsPathSafe(string? path, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                reason = "Path cannot be empty";
                return false;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                reason = $"Invalid path format: {ex.Message}";
                return false;
            }

            // Check if it's a root drive like "C:"
            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrEmpty(root))
            {
                string cleanRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(fullPath, cleanRoot, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Target cannot be a drive root";
                    return false;
                }
            }

            foreach (var protectedPrefix in _protectedPaths)
            {
                string cleanProtected = protectedPrefix.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(fullPath, cleanProtected, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(cleanProtected + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"Target path '{path}' is inside protected system folder '{protectedPrefix}'";
                    return false;
                }
            }

            return true;
        }
    }
}
