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
- **Resilient File Stream Engine**:
  - Non-blocking copying using `FileShare.ReadWrite` to safely read active/locked files.
  - Exponential backoff retry engine for busy or locked system files.
  - Read-only attribute stripping and non-blocking staging trash cleanup (`_trash_*`).
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

### Automation with Windows Task Scheduler

To automate backups, add an action in Windows Task Scheduler:
- **Program/script**: `C:\Program Files\RotateBackup\RotateBackup.exe`
- **Add arguments**: `client-backup`
- **Start in**: `C:\Program Files\RotateBackup`

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
