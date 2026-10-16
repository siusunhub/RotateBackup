# RotateBackup .NET 10 WPF Client & Headless CLI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a modern .NET 10 WPF desktop client with rounded native styling and a tabbed interface, a shared core rotation library, a headless CLI runner for Windows Task Scheduler, and a separate one-time migration utility for legacy LiteDB databases.

**Architecture:** A clean 4-project solution (`RotateBackup.sln`) where `RotateBackup.Core` holds the pure .NET 10 models, JSON storage, and rotation engine. `RotateBackup.Wpf` provides the desktop user experience with native rounded buttons and a single-window tabbed interface. `RotateBackup.Cli` provides a fast console runner (`RotateBackup.exe`). `RotateBackup.Migrator` isolates LiteDB legacy migration into a standalone CLI tool.

**Tech Stack:** C# 13 / .NET 10 (`net10.0`, `net10.0-windows`), WPF, `System.Text.Json`, xUnit for unit testing.

**Spec:** `docs/superpowers/specs/2026-10-16-rotatebackup-wpf-design.md`

## Global Constraints
- **Target Runtime**: .NET 10 (`net10.0` / `net10.0-windows`).
- **Compilation Mode**: Strictly Release mode (`dotnet build -c Release`).
- **Encoding**: UTF-8 without BOM or UTF-8 safe.
- **Dependencies**: Zero external dependencies in `RotateBackup.Core`, `RotateBackup.Wpf`, and `RotateBackup.Cli`. `LiteDB` (5.0.x) is strictly restricted to `RotateBackup.Migrator`.
- **Filesystem Safety**: Reject mutations to `C:\Windows`, `C:\Program Files`, `C:\Program Files (x86)`, and `C:\Users`.

## Review Focus
1. **Critical Path Safeguard**: Path pointing to `C:\Windows` or system root causes immediate rotation abort with a safety error.
2. **Pre-Command Failure**: If `preCommand` returns a non-zero exit code, rotation halts without moving or deleting any backup slots.
3. **Empty or Missing Config**: Missing `backup_settings.json` initializes gracefully to an empty config instead of crashing.
4. **Invalid Command Alias**: Alphanumeric + underscores only (no spaces allowed, matching legacy validation).
5. **Slot Retention Logic**: If a slot has `keep == true`, existing files in that slot are preserved rather than deleted during a roll.

---

### Task 1: Solution Setup, Core Models & JSON Storage

**Files:**
- Create: `RotateBackup.sln`
- Create: `src/RotateBackup.Core/RotateBackup.Core.csproj`
- Create: `src/RotateBackup.Core/Models/RotationSlot.cs`
- Create: `src/RotateBackup.Core/Models/BackupSet.cs`
- Create: `src/RotateBackup.Core/Models/BackupConfig.cs`
- Create: `src/RotateBackup.Core/Storage/JsonBackupRepository.cs`
- Test: `tests/RotateBackup.Core.Tests/RotateBackup.Core.Tests.csproj`
- Test: `tests/RotateBackup.Core.Tests/JsonRepositoryTests.cs`

**Interfaces:**
- Produces:
  - `class RotationSlot { int Index; string Path; bool Keep; }`
  - `class BackupSet { string Id; string Command; string Remark; bool Disabled; bool IsDirectory; string SourcePath; bool KeepSource; bool KeepFirstDir; string PreCommand; string PostCommand; List<RotationSlot> Slots; }`
  - `class BackupConfig { string Version; List<BackupSet> BackupSets; }`
  - `class JsonBackupRepository { BackupConfig Load(string path); void Save(BackupConfig config, string path); }`

- [ ] **Step 1: Write the failing tests for JSON serialization and round-trip loading**

```csharp
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
    string tempFile = Path.Combine(Path.GetTempPath(), $"test_backup_{Guid.NewGuid()}.json");
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: Project does not exist / build failure.

- [ ] **Step 3: Implement solution, projects, models, and `JsonBackupRepository`**
- Initialize `RotateBackup.sln`, `src/RotateBackup.Core`, and `tests/RotateBackup.Core.Tests`.
- Implement `BackupSet`, `RotationSlot`, and `BackupConfig`.
- Implement `JsonBackupRepository` with `JsonSerializerOptions { WriteIndented = true }`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: PASS.

---

### Task 2: Core Rotation Engine & Safety Validator

**Files:**
- Create: `src/RotateBackup.Core/Engine/SafetyValidator.cs`
- Create: `src/RotateBackup.Core/Engine/RotationLogger.cs`
- Create: `src/RotateBackup.Core/Engine/BackupRunner.cs`
- Test: `tests/RotateBackup.Core.Tests/SafetyValidatorTests.cs`
- Test: `tests/RotateBackup.Core.Tests/BackupRunnerTests.cs`

**Interfaces:**
- Consumes: `BackupSet`, `RotationSlot` from Task 1.
- Produces:
  - `class SafetyValidator { bool IsPathSafe(string path, out string reason); }`
  - `class BackupRunner { Task<bool> RunAsync(BackupSet set, Action<string> logCallback, CancellationToken ct = default); }`

- [ ] **Step 1: Write failing tests for SafetyValidator and BackupRunner directory rotation**

```csharp
[Theory]
[InlineData(@"C:\WINDOWS")]
[InlineData(@"C:\Windows\System32")]
[InlineData(@"C:\Program Files")]
[InlineData(@"C:\Program Files (x86)")]
[InlineData(@"C:\Users")]
public void SafetyValidator_RejectsSystemPaths(string path)
{
    var validator = new SafetyValidator();
    bool isSafe = validator.IsPathSafe(path, out var reason);
    Assert.False(isSafe);
    Assert.NotEmpty(reason);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: FAIL.

- [ ] **Step 3: Implement `SafetyValidator` and `BackupRunner`**
- In `SafetyValidator.cs`, check path normalized roots against system directories.
- In `BackupRunner.cs`:
  - Execute `PreCommand` via `ProcessStartInfo` if specified. Abort if exit code != 0.
  - For directory rotation: iterate slots backwards from $N$ to 1; delete non-keep destination if present, move slot $i-1$ to $i$; copy/move `sourcePath` to slot 1 based on `keepSource`/`keepFirstDir`.
  - For file rotation: roll files backwards and place source file into slot 1.
  - Execute `PostCommand` on success.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: PASS.

---

### Task 3: Headless CLI Runner (`RotateBackup.Cli`)

**Files:**
- Create: `src/RotateBackup.Cli/RotateBackup.Cli.csproj`
- Create: `src/RotateBackup.Cli/Program.cs`

**Interfaces:**
- Consumes: `JsonBackupRepository`, `BackupRunner` from Task 1 and 2.
- Produces: Executable `RotateBackup.exe` with command line `RotateBackup.exe <command_name>`.

- [ ] **Step 1: Write CLI implementation with argument handling and exit codes**
- Reads command name from `args[0]`.
- Prints usage if no argument is passed (Exit code 1).
- Loads `backup_settings.json` from the application directory.
- Matches `BackupSet` by `Command` (case-insensitive).
- If not found or disabled, prints error and exits (Exit code 2).
- Executes `BackupRunner.RunAsync()` streaming output to `Console.WriteLine`.
- Exits 0 on success, 3 on rotation error.

- [ ] **Step 2: Verify CLI builds and runs in Release mode**

Run: `dotnet build src/RotateBackup.Cli -c Release`
Expected: Build succeeded with 0 errors.

---

### Task 4: Standalone Migration Utility (`RotateBackup.Migrator`)

**Files:**
- Create: `src/RotateBackup.Migrator/RotateBackup.Migrator.csproj`
- Create: `src/RotateBackup.Migrator/LegacyBackupSetting.cs`
- Create: `src/RotateBackup.Migrator/Program.cs`
- Test: `tests/RotateBackup.Core.Tests/MigratorMappingTests.cs`

**Interfaces:**
- Consumes: `BackupSet`, `RotationSlot`, `BackupConfig`, `JsonBackupRepository` from Task 1.
- Dependencies: `LiteDB` 5.0.11+ in `RotateBackup.Migrator.csproj` only.

- [ ] **Step 1: Write failing test for legacy model to modern model conversion**

```csharp
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
    Assert.Equal(3, modern.Slots.Count);
    Assert.False(modern.Slots[0].Keep);
    Assert.True(modern.Slots[1].Keep);
    Assert.False(modern.Slots[2].Keep);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: FAIL.

- [ ] **Step 3: Implement `LegacyConverter` and `RotateBackup.Migrator`**
- Converts legacy `BackupSet.db` (LiteDB) records to `backup_settings.json`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/RotateBackup.Core.Tests -c Release`
Expected: PASS.

---

### Task 5: WPF Client Foundation, Native Rounded Styles & Theme Resources

**Files:**
- Create: `src/RotateBackup.Wpf/RotateBackup.Wpf.csproj`
- Create: `src/RotateBackup.Wpf/App.xaml`
- Create: `src/RotateBackup.Wpf/App.xaml.cs`
- Create: `src/RotateBackup.Wpf/Styles/ThemeColors.xaml`
- Create: `src/RotateBackup.Wpf/Styles/RoundedButtons.xaml`
- Create: `src/RotateBackup.Wpf/Styles/Inputs.xaml`
- Create: `src/RotateBackup.Wpf/Styles/TabControl.xaml`

**Interfaces:**
- Produces:
  - `Style x:Key="RoundedPrimaryButton"`
  - `Style x:Key="RoundedSecondaryButton"`
  - `Style x:Key="RoundedDangerButton"`
  - `Style x:Key="RoundedTextBox"`
  - `Style x:Key="RoundedComboBox"`
  - `Style x:Key="ModernTabControl"`

- [ ] **Step 1: Create `RotateBackup.Wpf.csproj` targeting `net10.0-windows` with `<UseWPF>true</UseWPF>`**
- [ ] **Step 2: Implement XAML styles for rounded buttons, smooth hover transitions, rounded inputs, and tab navigation**
- [ ] **Step 3: Build `RotateBackup.Wpf` to verify XAML parses without errors**

Run: `dotnet build src/RotateBackup.Wpf -c Release`
Expected: Build succeeded with 0 warnings/errors.

---

### Task 6: WPF ViewModels & MVVM Architecture

**Files:**
- Create: `src/RotateBackup.Wpf/ViewModels/ViewModelBase.cs`
- Create: `src/RotateBackup.Wpf/ViewModels/RelayCommand.cs`
- Create: `src/RotateBackup.Wpf/ViewModels/RotationSlotItemViewModel.cs`
- Create: `src/RotateBackup.Wpf/ViewModels/BackupSetViewModel.cs`
- Create: `src/RotateBackup.Wpf/ViewModels/MainViewModel.cs`

**Interfaces:**
- Consumes: `JsonBackupRepository`, `BackupRunner`, `BackupSet`, `RotationSlot` from `RotateBackup.Core`.
- Produces: Observable MVVM layer with commands:
  - `NewBackupSetCommand`, `DeleteBackupSetCommand`, `SaveAllCommand`
  - `AutoFillSlotsCommand`, `CreateMissingFoldersCommand`
  - `CopyCliCommand`, `RunTestBackupCommand`

- [ ] **Step 1: Implement `ViewModelBase` (INotifyPropertyChanged) and `RelayCommand`**
- [ ] **Step 2: Implement `BackupSetViewModel` wrapping `BackupSet` and observable collection of `RotationSlotItemViewModel`**
- [ ] **Step 3: Implement `MainViewModel` with auto-fill logic (`\1`, `\2`, ...), missing folder creation, and live execution logging**
- [ ] **Step 4: Build project to verify type consistency and compilation**

Run: `dotnet build src/RotateBackup.Wpf -c Release`
Expected: Build succeeded.

---

### Task 7: WPF Single-Window Tabbed UI

**Files:**
- Create: `src/RotateBackup.Wpf/Views/MainWindow.xaml`
- Create: `src/RotateBackup.Wpf/Views/MainWindow.xaml.cs`
- Create: `src/RotateBackup.Wpf/Views/NewBackupSetDialog.xaml`
- Create: `src/RotateBackup.Wpf/Views/NewBackupSetDialog.xaml.cs`

**Interfaces:**
- Consumes: Styles from Task 5, ViewModels from Task 6.
- Produces: Modern WPF window displaying:
  - Header: Application Title, status badge, Save button, New button.
  - Tab 1 (**Backup Sets**): Set selector list with icons/status, details card (Command, Remark, Directory/File, Disabled).
  - Tab 2 (**Rotation Paths**): Source path browse picker, reference path input, slot count picker, `[Auto-Fill 1..N]` rounded button, `[Create Folders]` rounded button, slot list with browse & keep flags.
  - Tab 3 (**Scripts & Advanced**): Pre/Post commands, Task Scheduler command box with `[📋 Copy]`, `[▶ Test Run Now]` button, and embedded live output log.

- [ ] **Step 1: Create `NewBackupSetDialog.xaml` modal dialog with validation**
- [ ] **Step 2: Create `MainWindow.xaml` with modern single-window tabbed interface and rounded styling**
- [ ] **Step 3: Hook folder and file browser dialogs using modern native Windows dialogs**
- [ ] **Step 4: Compile WPF in Release mode and ensure clean build**

Run: `dotnet build src/RotateBackup.Wpf -c Release`
Expected: Build succeeded with 0 errors.

---

### Task 8: End-to-End Build, Integration & Verification

**Files:**
- Verify: Full solution build
- Verify: CLI execution test
- Verify: Automated migration tool verification with existing database

- [ ] **Step 1: Build entire solution in Release mode**

Run: `dotnet build RotateBackup.sln -c Release`
Expected: All 4 projects build with 0 errors.

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test -c Release`
Expected: 100% test pass.

- [ ] **Step 3: Test CLI runner against mock backup set**

Run: Test `RotateBackup.Cli.exe` with a test directory rotation to verify exit codes, logging, and folder movement.

- [ ] **Step 4: Commit all changes**

```bash
git add .
git commit -m "feat: implement modern .NET 10 WPF RotateBackup client, CLI runner, and migrator"
```
