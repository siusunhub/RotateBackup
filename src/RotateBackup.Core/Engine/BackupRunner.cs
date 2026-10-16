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
            void Log(string message) => logCallback?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

            ArgumentNullException.ThrowIfNull(set);

            Log($"Starting backup task: '{set.Command}' ({set.Remark})");

            if (set.Disabled)
            {
                Log("Task is disabled in settings. Skipping.");
                return false;
            }

            // 1. Safety Validations
            if (!_safetyValidator.IsPathSafe(set.SourcePath, out string sourceReason))
            {
                Log($"[ERROR] Source path rejected: {sourceReason}");
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
                    Log($"[ERROR] Slot #{slot.Index} path rejected: {slotReason}");
                    return false;
                }
            }

            // 2. Pre-Command Execution
            if (!string.IsNullOrWhiteSpace(set.PreCommand))
            {
                Log($"Executing Pre-Command: {set.PreCommand}");
                int exitCode = await ExecuteShellCommandAsync(set.PreCommand, Log, ct);
                if (exitCode != 0)
                {
                    Log($"[ERROR] Pre-Command failed with exit code {exitCode}. Aborting rotation.");
                    return false;
                }
                Log("Pre-Command completed successfully.");
            }

            // 3. Execution (Directory or File)
            bool rotationSuccess = false;
            try
            {
                if (set.IsDirectory)
                {
                    rotationSuccess = await RotateDirectoriesAsync(set, validSlots, Log, ct);
                }
                else
                {
                    rotationSuccess = await RotateFilesAsync(set, validSlots, Log, ct);
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Rotation encountered an unhandled exception: {ex.Message}");
                return false;
            }

            if (!rotationSuccess)
            {
                Log("[ERROR] Rotation failed.");
                return false;
            }

            // 4. Post-Command Execution
            if (!string.IsNullOrWhiteSpace(set.PostCommand))
            {
                Log($"Executing Post-Command: {set.PostCommand}");
                int postExitCode = await ExecuteShellCommandAsync(set.PostCommand, Log, ct);
                if (postExitCode != 0)
                {
                    Log($"[WARNING] Post-Command returned non-zero exit code: {postExitCode}");
                }
                else
                {
                    Log("Post-Command completed successfully.");
                }
            }

            Log("Backup task completed successfully.");
            return true;
        }

        private async Task<bool> RotateDirectoriesAsync(BackupSet set, List<RotationSlot> slots, Action<string> log, CancellationToken ct)
        {
            if (!Directory.Exists(set.SourcePath))
            {
                log($"[ERROR] Source directory does not exist: {set.SourcePath}");
                return false;
            }

            if (slots.Count == 0)
            {
                log("[WARNING] No rotation slots configured.");
                return true;
            }

            // Roll backwards from slot N down to 2
            for (int i = slots.Count - 1; i >= 1; i--)
            {
                ct.ThrowIfCancellationRequested();
                var currentSlot = slots[i];       // target: slot i+1 (1-based)
                var previousSlot = slots[i - 1];   // source: slot i

                if (Directory.Exists(previousSlot.Path))
                {
                    log($"Rolling slot #{previousSlot.Index} -> #{currentSlot.Index}");

                    if (Directory.Exists(currentSlot.Path))
                    {
                        if (!currentSlot.Keep)
                        {
                            log($"Deleting non-keep target directory: {currentSlot.Path}");
                            Directory.Delete(currentSlot.Path, true);
                        }
                    }

                    if (!Directory.Exists(currentSlot.Path))
                    {
                        Directory.Move(previousSlot.Path, currentSlot.Path);
                    }
                    else
                    {
                        // If target directory is kept, copy contents instead
                        CopyDirectory(previousSlot.Path, currentSlot.Path);
                    }
                }
            }

            // Move or copy Source to Slot 1
            var firstSlot = slots[0];
            ct.ThrowIfCancellationRequested();

            if (Directory.Exists(firstSlot.Path))
            {
                if (!firstSlot.Keep)
                {
                    log($"Deleting non-keep slot #1 directory: {firstSlot.Path}");
                    Directory.Delete(firstSlot.Path, true);
                }
            }

            log($"Transferring source '{set.SourcePath}' -> slot #1 '{firstSlot.Path}' (KeepSource={set.KeepSource})");

            if (set.KeepSource)
            {
                if (!Directory.Exists(firstSlot.Path))
                {
                    Directory.CreateDirectory(firstSlot.Path);
                }
                CopyDirectory(set.SourcePath, firstSlot.Path);
            }
            else
            {
                if (Directory.Exists(firstSlot.Path))
                {
                    CopyDirectory(set.SourcePath, firstSlot.Path);
                    Directory.Delete(set.SourcePath, true);
                }
                else
                {
                    Directory.Move(set.SourcePath, firstSlot.Path);
                }
            }

            return true;
        }

        private async Task<bool> RotateFilesAsync(BackupSet set, List<RotationSlot> slots, Action<string> log, CancellationToken ct)
        {
            if (!File.Exists(set.SourcePath))
            {
                log($"[ERROR] Source file does not exist: {set.SourcePath}");
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
                    log($"Rolling file slot #{previousSlot.Index} -> #{currentSlot.Index}");
                    string? targetDir = Path.GetDirectoryName(currentSlot.Path);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    if (File.Exists(currentSlot.Path) && !currentSlot.Keep)
                    {
                        File.Delete(currentSlot.Path);
                    }

                    if (!File.Exists(currentSlot.Path))
                    {
                        File.Move(previousSlot.Path, currentSlot.Path);
                    }
                    else
                    {
                        File.Copy(previousSlot.Path, currentSlot.Path, true);
                    }
                }
            }

            // Move/copy source file to slot 1
            var firstSlot = slots[0];
            ct.ThrowIfCancellationRequested();

            string? slot1Dir = Path.GetDirectoryName(firstSlot.Path);
            if (!string.IsNullOrEmpty(slot1Dir) && !Directory.Exists(slot1Dir))
            {
                Directory.CreateDirectory(slot1Dir);
            }

            if (File.Exists(firstSlot.Path) && !firstSlot.Keep)
            {
                File.Delete(firstSlot.Path);
            }

            log($"Transferring source file '{set.SourcePath}' -> slot #1 '{firstSlot.Path}' (KeepSource={set.KeepSource})");
            if (set.KeepSource)
            {
                File.Copy(set.SourcePath, firstSlot.Path, true);
            }
            else
            {
                if (File.Exists(firstSlot.Path))
                {
                    File.Copy(set.SourcePath, firstSlot.Path, true);
                    File.Delete(set.SourcePath);
                }
                else
                {
                    File.Move(set.SourcePath, firstSlot.Path);
                }
            }

            return true;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
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
