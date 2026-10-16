# Specification: RotateBackup .NET 10 WPF Modern Client & CLI

## 1. Overview
Rebuild the `RotateBackup` backup management and execution system targeting **.NET 10**. 
The system replaces legacy WinForms (`RotateBackupSetting`) and old .NET Framework console runner with:
1. A modern, native-feeling WPF desktop client featuring rounded buttons, clean cards, and a single-window tabbed interface.
2. A lightweight headless CLI runner (`RotateBackup.Cli`) for automated Windows Task Scheduler executions.
3. Formatted JSON persistence (`System.Text.Json`) with zero third-party dependencies in the main runtime.
4. A dedicated, standalone migration utility (`RotateBackup.Migrator`) to convert existing LiteDB `BackupSet.db` files into the new JSON format without polluting the main client or runner.

---

## 2. Architecture & Projects

Solution: `RotateBackup.sln` inside `RotateBackup/`

### 2.1 `RotateBackup.Core` (.NET 10 Class Library)
- **Zero external dependencies** (pure .NET 10 runtime).
- **Models**:
  - `BackupSet`: Represents a configured backup task (ID, command alias, remark, type: Directory/File, disabled flag, source path, keep options, pre/post commands, slot collection).
  - `RotationSlot`: Index (1..N), target path, keep flag (whether to preserve previous contents during rotation).
  - `BackupConfig`: Root container holding all `BackupSet` definitions and metadata.
- **Storage**:
  - `JsonBackupRepository`: Thread-safe, indented JSON reader and writer using `System.Text.Json`. Defaults to `backup_settings.json` located next to the executable.
- **Engine**:
  - `BackupRunner`: Executes pre-command, performs file/folder rolling rotation, executes post-command, and provides event-based real-time log callbacks.
  - `SafetyValidator`: Validates paths against system folders (`C:\Windows`, `C:\Program Files`, root system drives) to guarantee data safety.

### 2.2 `RotateBackup.Wpf` (.NET 10 WPF Application)
- **Visual Design**:
  - Windows 11 Fluent aesthetic with Segoe UI Variable typography.
  - **Native Rounded Buttons**: Custom XAML control template with `CornerRadius="8"` / `"10"`, border highlights, and smooth hover/pressed state animations.
  - Rounded text inputs and combo boxes (`CornerRadius="6"`).
- **Single-Window Tabbed Interface**:
  - **Tab 1: Backup Sets**:
    - Left side/card: List of backup sets with quick status badge (Active / Disabled), type icon (📁 Directory / 📄 File), and action buttons `[+ New]`, `[Delete]`, `[Duplicate]`.
    - Right side: Details editor for command name (alphanumeric validation), remark, directory/file switch, and active toggle.
  - **Tab 2: Rotation Paths**:
    - Source path configuration (picker dialog, `keepSource`, `keepFirstDir`).
    - Quick generator toolbar: Reference path input, slot count selector (1..14+), `[Auto-Fill 1..N]` rounded button, `[Create Folders]` rounded button.
    - Slot list: Card/grid of slots with slot index badge, path input, browse button, and individual "Keep" toggle.
  - **Tab 3: Scripts & Advanced**:
    - Pre-command and Post-command text boxes.
    - Task Scheduler command snippet with `[📋 Copy Command]` button: `"RotateBackup.exe <command>"`.
    - `[▶ Test Run Now]` button with an integrated live execution log viewer.

### 2.3 `RotateBackup.Cli` (.NET 10 Console Application)
- Headless executable compiled as `RotateBackup.exe`.
- Syntax: `RotateBackup.exe <command_name>` (e.g. `RotateBackup.exe daily_sql`).
- Fast startup, reads `backup_settings.json`, executes rotation via `RotateBackup.Core`, and logs cleanly to stdout with proper exit codes (0 for success, 1+ for errors).

### 2.4 `RotateBackup.Migrator` (.NET 10 Standalone Console Tool)
- Standalone one-time migration utility referencing `LiteDB` 5.0.x.
- Reads legacy `BackupSet.db` (containing `Path1..Path14`, `keepPath1..14`, etc.).
- Converts legacy data into `backup_settings.json` format.
- Kept completely separate from the main runtime solution to ensure zero bloat in `Core`, `Wpf`, and `Cli`.

---

## 3. Data Specification (`backup_settings.json`)

```json
{
  "version": "2.0",
  "backupSets": [
    {
      "id": "A1B2C3D4E5F67890",
      "command": "daily_sql",
      "remark": "Daily MySQL Dump Rotation",
      "disabled": false,
      "isDirectory": true,
      "sourcePath": "D:\\Backups\\Current",
      "keepSource": false,
      "keepFirstDir": true,
      "preCommand": "net stop mysql",
      "postCommand": "net start mysql",
      "slots": [
        { "index": 1, "path": "D:\\Backups\\1", "keep": false },
        { "index": 2, "path": "D:\\Backups\\2", "keep": false },
        { "index": 3, "path": "D:\\Backups\\3", "keep": true }
      ]
    }
  ]
}
```

---

## 4. Safety & Error Handling
1. **Critical Path Check**: Rejects any rotation path targeting OS root folders (`C:\Windows`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\Users`).
2. **Pre-Command Exit Code**: If `preCommand` fails (non-zero exit code), the rotation is halted immediately, preserving existing backups.
3. **Atomic File Movement / Safety**: Handles directory locks and retries where appropriate; outputs descriptive error messages.

---

## 5. Build & Verification
- Compile strictly in **Release** configuration (`dotnet build -c Release`).
- Unit tests in `RotateBackup.Core.Tests`:
  - JSON serialization / deserialization round-trip.
  - Path safety validation rules.
  - Rotation mechanics (mock directory simulation).
  - Legacy LiteDB schema mapping verification.
