using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RotateBackup.Core.Models;

namespace RotateBackup.Core.Engine
{
    public class BackupRunner
    {
        private readonly SafetyValidator _safetyValidator;

        public BackupRunner(SafetyValidator? safetyValidator = null)
        {
            _safetyValidator = safetyValidator ?? new SafetyValidator();
        }

        public async Task<bool> RunAsync(BackupSet set, Action<string>? logCallback = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(set);

            var stopwatch = Stopwatch.StartNew();
            string? resolvedLogPath = null;
            object logLock = new object();

            if (set.EnableLog)
            {
                resolvedLogPath = !string.IsNullOrWhiteSpace(set.LogPath)
                    ? set.LogPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"{set.Command}.log");

                try
                {
                    string? logDir = Path.GetDirectoryName(resolvedLogPath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    lock (logLock)
                    {
                        File.AppendAllText(resolvedLogPath,
                            $"{Environment.NewLine}--------------------------------------------------------------------------------" +
                            $"{Environment.NewLine}[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [START] Starting Backup Job: '{set.Command}' ({set.Remark})" +
                            $"{Environment.NewLine}--------------------------------------------------------------------------------" +
                            $"{Environment.NewLine}", System.Text.Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] Could not initialize log file '{resolvedLogPath}': {ex.Message}");
                }
            }

            void Log(string message, bool writeToFile = true)
            {
                string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                logCallback?.Invoke(formatted);

                // For copying process: screen shows every line, but file log skips intermediate progress/verification lines
                if (writeToFile && (message.StartsWith("[COPY PROGRESS]", StringComparison.OrdinalIgnoreCase) ||
                                    message.StartsWith("[VERIFIED]", StringComparison.OrdinalIgnoreCase)))
                {
                    writeToFile = false;
                }

                if (writeToFile && set.EnableLog && !string.IsNullOrEmpty(resolvedLogPath))
                {
                    try
                    {
                        lock (logLock)
                        {
                            File.AppendAllText(resolvedLogPath, formatted + Environment.NewLine, System.Text.Encoding.UTF8);
                        }
                    }
                    catch
                    {
                        // Ensure logging failure does not interrupt the backup
                    }
                }
            }

            Action<string> log = msg => Log(msg, writeToFile: true);
            Action<string> screenOnlyLog = msg => Log(msg, writeToFile: false);

            Log($"[START] Starting backup task: '{set.Command}' ({set.Remark})");

            if (set.Disabled)
            {
                Log("[WARNING] Task is disabled in settings. Skipping execution.");
                return false;
            }

            if (set.EnableLog && !string.IsNullOrEmpty(resolvedLogPath))
            {
                Log($"[LOGGING] Activity log enabled -> File: '{resolvedLogPath}'");
            }

            // 1. Safety Validations
            if (!_safetyValidator.IsPathSafe(set.SourcePath, out string sourceReason))
            {
                Log($"[CRASH/ERROR] Source path rejected by safety check: {sourceReason}");
                return false;
            }

            var validSlots = set.Slots
                .Where(s => !string.IsNullOrWhiteSpace(s.Path))
                .OrderBy(s => s.Index)
                .ToList();

            foreach (var slot in validSlots)
            {
                if (!_safetyValidator.IsPathSafe(slot.Path, out string slotReason))
                {
                    Log($"[CRASH/ERROR] Slot #{slot.Index} path rejected by safety check: {slotReason}");
                    return false;
                }
            }

            Log($"[CONFIG] Mode: {(set.IsDirectory ? "Directory" : "File")}, Source: '{set.SourcePath}', KeepSource: {set.KeepSource}, Slots: {validSlots.Count}");

            // 2. Pre-Command Execution
            if (!string.IsNullOrWhiteSpace(set.PreCommand))
            {
                Log($"[PRE-COMMAND] Executing Pre-Command: {set.PreCommand}");
                int exitCode = await ExecuteShellCommandAsync(set.PreCommand, log, ct);
                if (exitCode != 0)
                {
                    Log($"[CRASH/ERROR] Pre-Command failed with exit code {exitCode}. Aborting rotation.");
                    return false;
                }
                Log("[PRE-COMMAND] Pre-Command completed successfully.");
            }

            // 3. Execution (Directory or File)
            bool rotationSuccess = false;
            try
            {
                if (set.IsDirectory)
                {
                    rotationSuccess = await RotateDirectoriesAsync(set, validSlots, log, screenOnlyLog, ct);
                }
                else
                {
                    rotationSuccess = await RotateFilesAsync(set, validSlots, log, screenOnlyLog, ct);
                }
            }
            catch (Exception ex)
            {
                Log($"[CRASH] Rotation encountered an unhandled exception: {ex.GetType().Name}: {ex.Message}");
                Log($"[CRASH STACKTRACE] {ex.StackTrace}");
                return false;
            }

            if (!rotationSuccess)
            {
                Log($"[FAILED] Backup task failed. Duration: {stopwatch.Elapsed.TotalSeconds:F2}s");
                return false;
            }

            // 4. Post-Command Execution
            if (!string.IsNullOrWhiteSpace(set.PostCommand))
            {
                Log($"[POST-COMMAND] Executing Post-Command: {set.PostCommand}");
                int postExitCode = await ExecuteShellCommandAsync(set.PostCommand, log, ct);
                if (postExitCode != 0)
                {
                    Log($"[WARNING] Post-Command returned non-zero exit code: {postExitCode}");
                }
                else
                {
                    Log("[POST-COMMAND] Post-Command completed successfully.");
                }
            }

            Log($"[COMPLETE] Backup task '{set.Command}' completed successfully. Duration: {stopwatch.Elapsed.TotalSeconds:F2}s");
            return true;
        }

        private async Task<bool> RotateDirectoriesAsync(
            BackupSet set,
            List<RotationSlot> slots,
            Action<string> log,
            Action<string> screenOnlyLog,
            CancellationToken ct)
        {
            if (!Directory.Exists(set.SourcePath))
            {
                log($"[CRASH/ERROR] Source directory does not exist: {set.SourcePath}");
                return false;
            }

            if (slots.Count == 0)
            {
                log("[WARNING] No rotation slots configured.");
                return true;
            }

            // Roll backwards from slot N down to 2 using fast atomic folder rename strategy
            for (int i = slots.Count - 1; i >= 1; i--)
            {
                ct.ThrowIfCancellationRequested();
                var currentSlot = slots[i];       // target: slot i+1 (1-based)
                var previousSlot = slots[i - 1];   // source: slot i

                await RollDirectorySlotAsync(previousSlot, currentSlot, log, screenOnlyLog, ct);
            }

            // Move or copy Source to Slot 1
            var firstSlot = slots[0];
            ct.ThrowIfCancellationRequested();

            if (Directory.Exists(firstSlot.Path) && !firstSlot.Keep)
            {
                log($"[CLEANUP] Deleting non-keep slot #1 directory: '{firstSlot.Path}'");
                ExecuteWithRetry(() => Directory.Delete(firstSlot.Path, true), $"Delete slot #1 directory '{firstSlot.Path}'", log);
            }

            if (set.KeepSource)
            {
                if (!Directory.Exists(firstSlot.Path))
                {
                    ExecuteWithRetry(() => Directory.CreateDirectory(firstSlot.Path), $"Create directory '{firstSlot.Path}'", log);
                }
                log($"[PROCESS DIR] Transferring source directory '{set.SourcePath}' -> slot #1 '{firstSlot.Path}' (KeepSource=True)");
                var sw = Stopwatch.StartNew();
                await CopyDirectoryAsync(set.SourcePath, firstSlot.Path, log, ct, screenOnlyLog);
                log($"[PROCESS DIR] Completed transferring source directory in {sw.Elapsed.TotalSeconds:F2}s");
            }
            else
            {
                if (Directory.Exists(firstSlot.Path))
                {
                    log($"[PROCESS DIR] Transferring source directory '{set.SourcePath}' -> slot #1 '{firstSlot.Path}' (KeepSource=False)");
                    var sw = Stopwatch.StartNew();
                    await CopyDirectoryAsync(set.SourcePath, firstSlot.Path, log, ct, screenOnlyLog);
                    log($"[PROCESS DIR] Completed transferring source directory in {sw.Elapsed.TotalSeconds:F2}s");
                    log($"[CLEANUP] Deleting source directory '{set.SourcePath}' (KeepSource is false)");
                    SafeDeleteDirectory(set.SourcePath, log);
                }
                else
                {
                    log($"[PROCESS DIR] Moving source directory '{set.SourcePath}' -> '{firstSlot.Path}'");
                    await MoveOrCopyDirectorySafelyAsync(set.SourcePath, firstSlot.Path, log, ct, screenOnlyLog);
                }
            }

            return true;
        }

        private static async Task RollDirectorySlotAsync(
            RotationSlot previousSlot,
            RotationSlot currentSlot,
            Action<string> log,
            Action<string> screenOnlyLog,
            CancellationToken ct)
        {
            if (string.Equals(previousSlot.Path, currentSlot.Path, StringComparison.OrdinalIgnoreCase))
            {
                log($"[WARNING] Slot #{previousSlot.Index} and slot #{currentSlot.Index} have the same path '{previousSlot.Path}'. Skipping.");
                return;
            }

            if (!Directory.Exists(previousSlot.Path))
            {
                return;
            }

            log($"[ROTATE DIR] Rolling slot #{previousSlot.Index} ('{previousSlot.Path}') -> #{currentSlot.Index} ('{currentSlot.Path}')");

            bool sameVolume = IsSameVolume(previousSlot.Path, currentSlot.Path);

            // If target directory exists and is NOT marked Keep, we must remove or replace it
            if (Directory.Exists(currentSlot.Path) && !currentSlot.Keep)
            {
                bool targetCleared = false;
                string? trashDir = null;

                // Ensure no read-only attribute blocks rename/delete
                ClearReadOnlyAttributes(currentSlot.Path);

                // Option A: Fast stage to temporary trash on the same volume
                if (sameVolume)
                {
                    trashDir = GetTrashDirectoryPath(currentSlot.Path);
                    log($"[RENAME STRATEGY] Staging old slot #{currentSlot.Index} to temporary trash: '{trashDir}'");
                    try
                    {
                        Directory.Move(currentSlot.Path, trashDir);
                        targetCleared = true;
                    }
                    catch (Exception ex)
                    {
                        log($"[WARNING] Direct stage to trash failed ({ex.Message}), falling back to safe delete...");
                        trashDir = null;
                    }
                }

                // Option B: If trash staging failed or cross-volume, delete target directly
                if (!targetCleared)
                {
                    SafeDeleteDirectory(currentSlot.Path, log);
                    // Give Windows filesystem a brief moment to clear directory entry
                    for (int wait = 0; wait < 10 && Directory.Exists(currentSlot.Path); wait++)
                    {
                        Thread.Sleep(50);
                    }
                }

                // Now attempt fast folder rename from previousSlot -> currentSlot
                if (sameVolume && !Directory.Exists(currentSlot.Path))
                {
                    string? parentDir = Path.GetDirectoryName(Path.GetFullPath(currentSlot.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        ExecuteWithRetry(() => Directory.CreateDirectory(parentDir), $"Create directory '{parentDir}'", log);
                    }

                    ClearReadOnlyAttributes(previousSlot.Path);
                    try
                    {
                        log($"[RENAME STRATEGY] Fast atomic folder rename: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                        Directory.Move(previousSlot.Path, currentSlot.Path);

                        // Purge staged trash now that the rename has succeeded
                        if (trashDir != null && Directory.Exists(trashDir))
                        {
                            log($"[CLEANUP] Purging staged trash folder: '{trashDir}'");
                            SafeDeleteDirectory(trashDir, log);
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        log($"[WARNING] Direct folder rename failed ({ex.Message}), falling back to content transfer...");
                    }
                }

                // Clean up trash if still present
                if (trashDir != null && Directory.Exists(trashDir))
                {
                    SafeDeleteDirectory(trashDir, log);
                }
            }
            else if (!Directory.Exists(currentSlot.Path) && sameVolume)
            {
                // Target slot does not exist yet: fast atomic rename directly
                string? parentDir = Path.GetDirectoryName(Path.GetFullPath(currentSlot.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    ExecuteWithRetry(() => Directory.CreateDirectory(parentDir), $"Create directory '{parentDir}'", log);
                }

                ClearReadOnlyAttributes(previousSlot.Path);
                try
                {
                    log($"[RENAME STRATEGY] Fast atomic folder rename: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                    Directory.Move(previousSlot.Path, currentSlot.Path);
                    return;
                }
                catch (Exception ex)
                {
                    log($"[WARNING] Direct folder rename failed ({ex.Message}), falling back to content transfer...");
                }
            }

            // Fallback: If direct folder rename couldn't be used (e.g. Keep=true, cross-volume, or filesystem ACL restricted folder rename)
            if (!Directory.Exists(currentSlot.Path))
            {
                log($"[PROCESS DIR] Moving folder contents: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                await MoveOrCopyDirectorySafelyAsync(previousSlot.Path, currentSlot.Path, log, ct, screenOnlyLog);
            }
            else
            {
                log($"[PROCESS DIR] Target exists (Keep={currentSlot.Keep}), copying contents: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                var sw = Stopwatch.StartNew();
                await CopyDirectoryAsync(previousSlot.Path, currentSlot.Path, log, ct, screenOnlyLog);
                log($"[PROCESS DIR] Completed copying contents in {sw.Elapsed.TotalSeconds:F2}s");
                if (!currentSlot.Keep)
                {
                    SafeDeleteDirectory(previousSlot.Path, log);
                }
            }
        }

        private static string GetTrashDirectoryPath(string path)
        {
            string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string? parent = Path.GetDirectoryName(trimmed);
            string name = Path.GetFileName(trimmed);
            string unique = Guid.NewGuid().ToString("N")[..8];
            return string.IsNullOrEmpty(parent)
                ? $"{trimmed}_trash_{unique}"
                : Path.Combine(parent, $"_trash_{name}_{unique}");
        }

        private static void SafeDeleteDirectory(string path, Action<string> log)
        {
            if (!Directory.Exists(path)) return;

            ClearReadOnlyAttributes(path);

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    if (!Directory.Exists(path)) return;
                    Directory.Delete(path, true);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return; // Already deleted
                }
                catch (FileNotFoundException)
                {
                    return; // Already deleted
                }
                catch (Exception ex) when (attempt < 3 && (ex is IOException || ex is UnauthorizedAccessException))
                {
                    log($"[RETRY] Delete directory '{path}' attempt {attempt}/3 failed ({ex.Message}), retrying...");
                    ClearReadOnlyAttributes(path);
                    Thread.Sleep(300);
                }
                catch (Exception ex)
                {
                    log($"[WARNING] Standard Directory.Delete failed for '{path}' ({ex.Message}), cleaning contents item-by-item...");
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            var di = new DirectoryInfo(path);
                            foreach (var file in di.GetFiles())
                            {
                                try { file.Attributes = FileAttributes.Normal; file.Delete(); } catch { }
                            }
                            foreach (var sub in di.GetDirectories())
                            {
                                try { ClearReadOnlyAttributes(sub.FullName); sub.Delete(true); } catch { }
                            }
                            di.Delete(true);
                        }
                    }
                    catch (Exception ex2)
                    {
                        log($"[WARNING] Fallback directory delete encountered: {ex2.Message}");
                    }
                    return;
                }
            }
        }

        private static void ClearReadOnlyAttributes(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var di = new DirectoryInfo(path);
                    if ((di.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        di.Attributes = FileAttributes.Directory;
                    }

                    foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                            {
                                file.Attributes = FileAttributes.Normal;
                            }
                        }
                        catch { }
                    }

                    foreach (var sub in di.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if ((sub.Attributes & FileAttributes.ReadOnly) != 0)
                            {
                                sub.Attributes = FileAttributes.Directory;
                            }
                        }
                        catch { }
                    }
                }
                else if (File.Exists(path))
                {
                    var fi = new FileInfo(path);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        fi.Attributes = FileAttributes.Normal;
                    }
                }
            }
            catch { }
        }

        private async Task<bool> RotateFilesAsync(
            BackupSet set,
            List<RotationSlot> slots,
            Action<string> log,
            Action<string> screenOnlyLog,
            CancellationToken ct)
        {
            if (!File.Exists(set.SourcePath))
            {
                log($"[CRASH/ERROR] Source file does not exist: {set.SourcePath}");
                return false;
            }

            if (slots.Count == 0)
            {
                log("[WARNING] No rotation slots configured.");
                return true;
            }

            // Roll backwards
            for (int i = slots.Count - 1; i >= 1; i--)
            {
                ct.ThrowIfCancellationRequested();
                var currentSlot = slots[i];
                var previousSlot = slots[i - 1];

                if (File.Exists(previousSlot.Path))
                {
                    log($"[ROTATE FILE] Rolling file slot #{previousSlot.Index} ('{previousSlot.Path}') -> #{currentSlot.Index} ('{currentSlot.Path}')");
                    string? targetDir = Path.GetDirectoryName(currentSlot.Path);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        ExecuteWithRetry(() => Directory.CreateDirectory(targetDir), $"Create directory '{targetDir}'", log);
                    }

                    bool sameVolume = IsSameVolume(previousSlot.Path, currentSlot.Path);
                    if (sameVolume && !currentSlot.Keep)
                    {
                        log($"[RENAME STRATEGY] Fast atomic rename file on same volume: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                        ExecuteWithRetry(() => File.Move(previousSlot.Path, currentSlot.Path, overwrite: true), $"Move file '{previousSlot.Path}' to '{currentSlot.Path}'", log);
                    }
                    else
                    {
                        if (File.Exists(currentSlot.Path) && !currentSlot.Keep)
                        {
                            log($"[CLEANUP] Deleting non-keep file: '{currentSlot.Path}'");
                            ExecuteWithRetry(() => File.Delete(currentSlot.Path), $"Delete file '{currentSlot.Path}'", log);
                        }

                        if (!File.Exists(currentSlot.Path) && sameVolume)
                        {
                            log($"[RENAME STRATEGY] Fast atomic rename file on same volume: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                            ExecuteWithRetry(() => File.Move(previousSlot.Path, currentSlot.Path), $"Move file '{previousSlot.Path}' to '{currentSlot.Path}'", log);
                        }
                        else
                        {
                            log($"[PROCESS FILE] Transferring file: '{previousSlot.Path}' -> '{currentSlot.Path}'");
                            var rollSw = Stopwatch.StartNew();
                            await CopyFileWithRetryAndProgressAsync(previousSlot.Path, currentSlot.Path, log, ct);
                            log($"[PROCESS FILE] Completed transferring '{Path.GetFileName(previousSlot.Path)}' in {rollSw.Elapsed.TotalSeconds:F2}s");
                            if (!File.Exists(currentSlot.Path) || !sameVolume)
                            {
                                ExecuteWithRetry(() => File.Delete(previousSlot.Path), $"Delete source slot file '{previousSlot.Path}'", log);
                            }
                        }
                    }
                }
            }

            // Move/copy source file to slot 1
            var firstSlot = slots[0];
            ct.ThrowIfCancellationRequested();

            string? slot1Dir = Path.GetDirectoryName(firstSlot.Path);
            if (!string.IsNullOrEmpty(slot1Dir) && !Directory.Exists(slot1Dir))
            {
                ExecuteWithRetry(() => Directory.CreateDirectory(slot1Dir), $"Create directory '{slot1Dir}'", log);
            }

            if (File.Exists(firstSlot.Path) && !firstSlot.Keep)
            {
                log($"[CLEANUP] Deleting non-keep slot #1 file: '{firstSlot.Path}'");
                ExecuteWithRetry(() => File.Delete(firstSlot.Path), $"Delete file '{firstSlot.Path}'", log);
            }

            long sourceLength = new FileInfo(set.SourcePath).Length;
            log($"[PROCESS FILE] Transferring source file '{set.SourcePath}' -> slot #1 '{firstSlot.Path}' ({FormatBytes(sourceLength)}, KeepSource={set.KeepSource})");

            bool sourceSameVolume = IsSameVolume(set.SourcePath, firstSlot.Path);
            if (!set.KeepSource && !File.Exists(firstSlot.Path) && sourceSameVolume)
            {
                // Fast atomic move on same volume
                log($"[PROCESS FILE] Fast-moving source file to slot #1: '{set.SourcePath}' -> '{firstSlot.Path}'");
                ExecuteWithRetry(() => File.Move(set.SourcePath, firstSlot.Path), $"Move source file '{set.SourcePath}' to '{firstSlot.Path}'", log);
            }
            else
            {
                // Robust buffered stream copy with progress, retry, disk space validation
                var copySw = Stopwatch.StartNew();
                await CopyFileWithRetryAndProgressAsync(set.SourcePath, firstSlot.Path, log, ct);
                log($"[PROCESS FILE] Completed transferring source file in {copySw.Elapsed.TotalSeconds:F2}s");

                if (!set.KeepSource)
                {
                    log($"[CLEANUP] Deleting source file '{set.SourcePath}' (KeepSource is false)");
                    ExecuteWithRetry(() => File.Delete(set.SourcePath), $"Delete source file '{set.SourcePath}'", log);
                }
            }

            return true;
        }

        private static async Task CopyDirectoryAsync(
            string sourceDir,
            string destinationDir,
            Action<string> log,
            CancellationToken ct,
            Action<string>? screenOnlyLog = null)
        {
            var screenLog = screenOnlyLog ?? log;
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            if (!Directory.Exists(destinationDir))
            {
                ExecuteWithRetry(() => Directory.CreateDirectory(destinationDir), $"Create directory '{destinationDir}'", log);
            }

            foreach (var file in dir.GetFiles())
            {
                ct.ThrowIfCancellationRequested();
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                // Intermediate individual files: show on screen only to avoid massive log file inflation
                screenLog($"[PROCESS FILE] Transferring file '{file.FullName}' -> '{targetFilePath}' ({FormatBytes(file.Length)})");
                await CopyFileWithRetryAndProgressAsync(file.FullName, targetFilePath, log, ct);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                ct.ThrowIfCancellationRequested();
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                screenLog($"[PROCESS DIR] Entering directory '{subDir.FullName}' -> '{newDestinationDir}'");
                await CopyDirectoryAsync(subDir.FullName, newDestinationDir, log, ct, screenLog);
            }
        }

        private static async Task MoveOrCopyDirectorySafelyAsync(
            string sourceDir,
            string destinationDir,
            Action<string> log,
            CancellationToken ct,
            Action<string>? screenOnlyLog = null)
        {
            if (IsSameVolume(sourceDir, destinationDir) && !Directory.Exists(destinationDir))
            {
                try
                {
                    Directory.Move(sourceDir, destinationDir);
                    return;
                }
                catch (Exception ex)
                {
                    log($"[WARNING] Direct directory move failed ({ex.Message}), falling back to robust copy-and-delete...");
                }
            }

            // Cross-volume or existing target directory: recursive copy then delete
            var sw = Stopwatch.StartNew();
            log($"[PROCESS DIR] Copying directory '{sourceDir}' -> '{destinationDir}'");
            await CopyDirectoryAsync(sourceDir, destinationDir, log, ct, screenOnlyLog);
            log($"[PROCESS DIR] Completed copying directory in {sw.Elapsed.TotalSeconds:F2}s");
            SafeDeleteDirectory(sourceDir, log);
        }

        /// <summary>
        /// High-performance, large-file friendly copy with:
        /// 1. Disk space check
        /// 2. FileShare.ReadWrite to avoid sharing violation crashes
        /// 3. 4MB chunked streaming with periodic progress logs
        /// 4. Auto retry policy with progressive delays (2s, 5s, 10s)
        /// 5. Size verification and timestamp preservation
        /// </summary>
        public static async Task CopyFileWithRetryAndProgressAsync(
            string sourcePath,
            string destinationPath,
            Action<string> log,
            CancellationToken ct,
            int maxRetries = 3)
        {
            var sourceInfo = new FileInfo(sourcePath);
            if (!sourceInfo.Exists)
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);
            }

            long sourceLength = sourceInfo.Length;

            // 1. Pre-copy Disk Space Validation
            CheckDiskSpace(destinationPath, sourceLength, log);

            int[] backoffDelays = { 2000, 5000, 10000 };

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await PerformStreamCopyAsync(sourcePath, destinationPath, sourceLength, log, ct);

                    // Post-copy verification
                    var destInfo = new FileInfo(destinationPath);
                    if (!destInfo.Exists)
                    {
                        throw new IOException($"Target file was not created: '{destinationPath}'");
                    }
                    if (destInfo.Length != sourceLength)
                    {
                        throw new IOException($"File size mismatch! Source: {sourceLength} bytes, Target: {destInfo.Length} bytes.");
                    }

                    // Preserve original timestamps
                    try
                    {
                        File.SetCreationTimeUtc(destinationPath, sourceInfo.CreationTimeUtc);
                        File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
                    }
                    catch
                    {
                        // Non-critical if filesystem doesn't support timestamp modification
                    }

                    log($"[VERIFIED] File '{Path.GetFileName(destinationPath)}' ({FormatBytes(sourceLength)}) transferred and verified successfully.");
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries && (ex is IOException || ex is UnauthorizedAccessException))
                {
                    int delayMs = attempt <= backoffDelays.Length ? backoffDelays[attempt - 1] : 5000;
                    log($"[RETRY] Attempt {attempt}/{maxRetries} failed for '{Path.GetFileName(sourcePath)}': {ex.GetType().Name} - {ex.Message}. Retrying in {delayMs / 1000}s...");

                    // Clean up partial target file
                    try
                    {
                        if (File.Exists(destinationPath)) File.Delete(destinationPath);
                    }
                    catch
                    {
                        // Best effort cleanup
                    }

                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    // Clean up corrupted file on final failure
                    try
                    {
                        if (File.Exists(destinationPath)) File.Delete(destinationPath);
                    }
                    catch
                    {
                        // Best effort cleanup
                    }

                    log($"[CRASH/ERROR] Fatal failure copying '{Path.GetFileName(sourcePath)}' on attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
                    throw;
                }
            }
        }

        private static async Task PerformStreamCopyAsync(
            string sourcePath,
            string destinationPath,
            long totalBytes,
            Action<string> log,
            CancellationToken ct)
        {
            string? targetDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            const int bufferSize = 4 * 1024 * 1024; // 4MB buffer for high-throughput disk I/O
            byte[] buffer = new byte[bufferSize];

            var sw = Stopwatch.StartNew();
            var lastProgressReport = Stopwatch.StartNew();
            long copiedBytes = 0;
            long lastCopiedBytes = 0;

            // Open source with ReadWrite share to prevent crashes if file is open by another process
            using var sourceStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous);

            using var destStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous);

            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, ct);
                copiedBytes += bytesRead;

                // Log periodic progress for large files (>= 50 MB) every 3 seconds
                if (totalBytes >= 50 * 1024 * 1024 && (lastProgressReport.ElapsedMilliseconds >= 3000 || copiedBytes == totalBytes))
                {
                    double elapsedSec = lastProgressReport.Elapsed.TotalSeconds;
                    long bytesDelta = copiedBytes - lastCopiedBytes;
                    double speedMBs = elapsedSec > 0 ? (bytesDelta / (1024.0 * 1024.0)) / elapsedSec : 0;
                    double percent = totalBytes > 0 ? (copiedBytes * 100.0 / totalBytes) : 100.0;

                    log($"[COPY PROGRESS] '{Path.GetFileName(sourcePath)}': {FormatBytes(copiedBytes)} / {FormatBytes(totalBytes)} ({percent:F1}%) at {speedMBs:F1} MB/s");

                    lastProgressReport.Restart();
                    lastCopiedBytes = copiedBytes;
                }
            }

            await destStream.FlushAsync(ct);
        }

        private static void CheckDiskSpace(string destinationPath, long requiredBytes, Action<string> log)
        {
            try
            {
                string fullPath = Path.GetFullPath(destinationPath);
                string? root = Path.GetPathRoot(fullPath);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady)
                    {
                        // Require at least 20MB overhead
                        long safetyBuffer = 20 * 1024 * 1024;
                        if (drive.AvailableFreeSpace < (requiredBytes + safetyBuffer))
                        {
                            string msg = $"Insufficient disk space on drive '{drive.Name}'. Required: {FormatBytes(requiredBytes)}, Available Free Space: {FormatBytes(drive.AvailableFreeSpace)}.";
                            log($"[CRASH/ERROR] {msg}");
                            throw new IOException(msg);
                        }
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // DriveInfo might fail on some UNC or custom network shares, log warning and proceed
                log($"[WARNING] Could not check free disk space on target destination: {ex.Message}");
            }
        }

        private static bool IsSameVolume(string path1, string path2)
        {
            try
            {
                string root1 = Path.GetPathRoot(Path.GetFullPath(path1)) ?? string.Empty;
                string root2 = Path.GetPathRoot(Path.GetFullPath(path2)) ?? string.Empty;
                return string.Equals(root1, root2, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L * 1024L)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0):F2} TB";
            if (bytes >= 1024L * 1024L * 1024L)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (bytes >= 1024L * 1024L)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            if (bytes >= 1024L)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private static void ExecuteWithRetry(Action action, string operationName, Action<string> log, int maxRetries = 3, int delayMs = 500)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex) when (operationName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) && (ex is DirectoryNotFoundException || ex is FileNotFoundException))
                {
                    // Target already does not exist, delete operation is satisfied
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries && (ex is IOException || ex is UnauthorizedAccessException))
                {
                    log($"[RETRY] Attempt {attempt}/{maxRetries} failed for '{operationName}': {ex.Message}. Retrying in {delayMs}ms...");
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    log($"[CRASH/ERROR] Failure during '{operationName}': {ex.GetType().Name}: {ex.Message}");
                    throw;
                }
            }
        }

        private static async Task<int> ExecuteShellCommandAsync(string command, Action<string> log, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) log($"[CMD OUT] {e.Data}");
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) log($"[CMD ERR] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);
            return process.ExitCode;
        }
    }
}
