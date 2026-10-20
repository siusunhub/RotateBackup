# RotateBackup

<p align="left">
  <img src="https://img.shields.io/badge/version-v0.2-0284C7?style=for-the-badge&logo=git&logoColor=white" alt="Version v0.2" />
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Platform Windows x64" />
  <img src="https://img.shields.io/badge/build-single--file%20executable-10B981?style=for-the-badge&logo=powershell&logoColor=white" alt="Single-File Lightweight" />
  <img src="https://img.shields.io/badge/license-MIT-64748B?style=for-the-badge" alt="License MIT" />
</p>

**RotateBackup** is a high-performance, lightweight rotating backup automation tool and visual management suite designed for Windows x64. It provides scheduled rotational directory/file archiving with atomic folder shifts, resilient file stream copying, and an intuitive modern WPF manager.

---

## What's New in v0.2

- **Atomic Folder Rename Rotation Strategy**: Directory slots (e.g., `1 -> 2 -> 3 ... -> N`) are rolled instantaneously using atomic filesystem rename operations instead of slow file-by-file duplication.
- **Smart Activity Logging**:
  - **Live Screen Stream**: Displays real-time progress for all files, streaming rates (MB/s), and `[VERIFIED]` tags during execution.
  - **Concise Log Files**: Disk logs record only milestone events, start/completion summaries, and critical warnings/errors, avoiding bloated multi-gigabyte log files.
- **Dynamic Slot & Roll Management**: Supports dynamic expansion and contraction of rotation slots (2 to 99+) with automatic target path generation.
- **Execution Log (Start / End / Result)**:
  - **Single shared log file** (`logs/execution.log`) that records when every backup started, when it ended and whether it succeeded or failed.
  - **Multi-process safe**: the CLI runner, the WPF manager and Task Scheduler jobs can write to the log at the same time without corrupting or interleaving lines (cross-process lock file, `FileShare.ReadWrite` reads).
  - **Manager section**: a new *Execution Log* tab per backup set shows the previous executions of the selected set, and *General Settings* has a one-click cleanup of entries older than 30 days.
- **Resilient File Stream Engine**:
  - Non-blocking copying using `FileShare.ReadWrite` to safely read active/locked files.
  - Exponential backoff retry engine for busy or locked system files.
  - Read-only attribute stripping and non-blocking staging trash cleanup (`_trash_*`).
- **Optional CRC-32 Integrity Check**: per backup set, the copied file is re-read from disk and its CRC-32 checksum is compared with the checksum of the transferred bytes. A mismatch fails the copy and triggers the retry policy.
- **Ultra-Lightweight Single-File Binaries**:
  - `RotateBackup.exe` (CLI Runner): **~350 KB**
  - `RotateBackupSetting.exe` (WPF Manager): **~800 KB**
- **Modern WPF GUI Redesign**:
  - Embedded high-resolution native branding.
  - Status bar with live `v0.2` release badge and set counters.
  - Tabbed workflow: Sets, General Settings, and Task Scheduler.

---

## Architecture & Components

| Component | Target Binary | Description |
| :--- | :--- | :--- |
| **RotateBackup.Cli** | `RotateBackup.exe` | Fast console runner ideal for Windows Task Scheduler, background services, and automated scripts. |
| **RotateBackup.Wpf** | `RotateBackupSetting.exe` | Modern desktop management suite with fluent design, configuration editor, and direct task execution. |
| **RotateBackup.Core** | `RotateBackup.Core.dll` | Engine library containing the backup runner, rotation logic, retry policies, and JSON repositories. |
| **RotateBackup.Migrator** | `RotateBackup.Migrator.exe` | One-click migration utility to import legacy `BackupSet.db` (LiteDB) configurations into JSON. |

---

## Configuration (`backup_settings.json`)

RotateBackup stores configuration in human-readable, UTF-8 encoded JSON format:

```json
{
  "Version": 2,
  "LastModified": "2026-10-19T17:00:00Z",
  "General": {
    "AutoCheckUpdate": false,
    "RunAtStartup": false,
    "DefaultLogDirectory": "logs"
  },
  "BackupSets": [
    {
      "Id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "Name": "Customer Database Backup",
      "Command": "client-backup",
      "Description": "Daily rotation for customer SQL and file archives",
      "Mode": "Directory",
      "SourcePath": "D:\\Database\\Backups",
      "DestinationPath": "E:\\RotationalBackups\\CustomerDB",
      "Slots": 5,
      "KeepSource": true,
      "Disabled": false,
      "EnableLogging": true,
      "LogPath": "C:\\Program Files\\RotateBackup\\logs\\client-backup.log",
      "EnableCrcCheck": false,
      "RotationStrategy": "Rename"
    }
  ]
}
```

---

## CLI Usage

Run a specific backup task by its command name:

```powershell
# Run the 'client-backup' task
RotateBackup.exe client-backup

# Show help & version
RotateBackup.exe --help
```

### Execution Log

Every execution is recorded in one shared file (`logs/execution.log`) next to `backup_settings.json`.
Each entry stores the **start time**, the **end time**, the **duration** and the **result**
(`SUCCESS` / `FAILED` / `RUNNING`) of the execution.

```powershell
# List the 25 most recent executions of every job
RotateBackup.exe logs

# List the executions of a single job
RotateBackup.exe logs client-backup --limit 10

# Show the details of the most recent execution
RotateBackup.exe logs client-backup --latest

# Remove entries older than 30 days (same as the button in the manager)
RotateBackup.exe logs --cleanup 30
```

> The log file is written by several programs at the same time (CLI, manager, Task Scheduler).
> Every append is serialized with a small cross-process lock file, so entries never interleave
> and are never lost. Reading is safe while another program is still writing.

### Automation with Windows Task Scheduler

To automate backups, add an action in Windows Task Scheduler:
- **Program/script**: `C:\Program Files\RotateBackup\RotateBackup.exe`
- **Add arguments**: `client-backup`
- **Start in**: `C:\Program Files\RotateBackup`

---

## Command Name Rules

The command name of a backup set is used as the CLI argument, in the Windows Task Scheduler
command line and as a field of the execution log, so it is restricted to simple, readable
characters:

| Allowed | Rejected |
| :--- | :--- |
| English letters `A-Z` `a-z` | spaces |
| Numbers `0-9` | non-ASCII characters (`每日`, `café`) |
| Underscore `_` | shell and path characters (`&`, `|`, `\`, `/`, `<`, `>`, `*`, `?`, `"`) |
| Hyphen `-` | empty names, names longer than 64 characters |

```powershell
RotateBackup.exe daily_sql      # valid
RotateBackup.exe client-backup  # valid
RotateBackup.exe "daily sql"    # rejected: [ERROR] Command name may only contain English letters, numbers, '_' and '-' ...
```

The manager validates the field live: an invalid name is highlighted in red with an explanation
below the input, and **Save All Changes** refuses to write an invalid configuration.

---

## CRC-32 Integrity Check (per backup set)

*General Settings* of every backup set has a **CRC Check for file copy complete** checkbox (placed
directly above *Disable this backup set*). When enabled:

1. The CRC-32 checksum of the source is computed on the fly while the file is being copied
   (no extra read - the disk is the bottleneck long before the CPU is).
2. The copied file is **re-read from disk** and its CRC-32 is computed again.
3. Both checksums are compared. On a mismatch the copy is deleted and retried
   (2s / 5s / 10s backoff), and the backup is reported as failed if it still does not match.

```
[CRC CHECK] CRC32 verified for 'lumenarts.zip': 1301647F
[VERIFIED] File 'lumenarts.zip' (327.86 MB) transferred and verified successfully. CRC32: 1301647F
```

The implementation is a self-contained slice-by-8 CRC-32 (no external dependency, so the single
file executables stay small) and produces the same checksums as `zlib` compatible tools.

> **Cost:** one extra sequential read of every copied file. Enable it for critical backups and
> leave it off for very large data sets where the extra read is not worth it.

---

## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Windows 10/11 or Windows Server 2019+

### Build in Release Mode
```powershell
dotnet build -c Release
```

### Publish Lightweight Single-File Executables
Publish binaries without bundling the 60MB+ C# runtime:

```powershell
# Publish CLI Runner (~350 KB)
dotnet publish src/RotateBackup.Cli/RotateBackup.Cli.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

# Publish WPF Manager (~800 KB)
dotnet publish src/RotateBackup.Wpf/RotateBackup.Wpf.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

# Publish Migrator
dotnet publish src/RotateBackup.Migrator/RotateBackup.Migrator.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

---

## License

This project is licensed under the [MIT License](LICENSE).
