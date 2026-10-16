using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RotateBackup.Core.Engine;
using RotateBackup.Core.Models;
using RotateBackup.Core.Storage;

namespace RotateBackup.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly JsonBackupRepository _repository;
        private readonly string _configFilePath;
        private BackupSetViewModel? _selectedBackupSet;
        private string _referencePath = string.Empty;
        private int _slotCountSelection = 5;
        private bool _isRunningBackup;
        private string _statusMessage = "Ready";

        public MainViewModel()
        {
            _repository = new JsonBackupRepository();
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup_settings.json");

            BackupSets = new ObservableCollection<BackupSetViewModel>();
            ExecutionLogs = new ObservableCollection<string>();

            // Initialize RelayCommands
            NewBackupSetCommand = new RelayCommand(ExecuteNewBackupSet);
            DeleteBackupSetCommand = new RelayCommand(ExecuteDeleteBackupSet, () => SelectedBackupSet != null);
            SaveAllCommand = new RelayCommand(ExecuteSaveAll);
            AutoFillSlotsCommand = new RelayCommand(ExecuteAutoFillSlots, () => SelectedBackupSet != null);
            CreateMissingFoldersCommand = new RelayCommand(ExecuteCreateMissingFolders, () => SelectedBackupSet != null);
            CopyCliSnippetCommand = new RelayCommand(() => ExecuteCopyCliSnippetCommand(), () => SelectedBackupSet != null && !string.IsNullOrWhiteSpace(SelectedBackupSet.Command));
            TestRunBackupCommand = new RelayCommand(async () => await ExecuteTestRunBackupAsync(), () => SelectedBackupSet != null && !IsRunningBackup);
            ClearLogsCommand = new RelayCommand(() => ExecutionLogs.Clear());

            LoadConfiguration();
        }

        public ObservableCollection<BackupSetViewModel> BackupSets { get; }
        public ObservableCollection<string> ExecutionLogs { get; }

        public BackupSetViewModel? SelectedBackupSet
        {
            get => _selectedBackupSet;
            set
            {
                if (SetProperty(ref _selectedBackupSet, value))
                {
                    OnPropertyChanged(nameof(HasSelectedBackupSet));
                    if (_selectedBackupSet != null && _selectedBackupSet.Slots.Count > 0)
                    {
                        SlotCountSelection = _selectedBackupSet.Slots.Count;
                    }
                }
            }
        }

        public bool HasSelectedBackupSet => SelectedBackupSet != null;

        public string ReferencePath
        {
            get => _referencePath;
            set => SetProperty(ref _referencePath, value);
        }

        public int SlotCountSelection
        {
            get => _slotCountSelection;
            set => SetProperty(ref _slotCountSelection, value);
        }

        public bool IsRunningBackup
        {
            get => _isRunningBackup;
            set => SetProperty(ref _isRunningBackup, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public RelayCommand NewBackupSetCommand { get; }
        public RelayCommand DeleteBackupSetCommand { get; }
        public RelayCommand SaveAllCommand { get; }
        public RelayCommand AutoFillSlotsCommand { get; }
        public RelayCommand CreateMissingFoldersCommand { get; }
        public RelayCommand CopyCliSnippetCommand { get; }
        public RelayCommand TestRunBackupCommand { get; }
        public RelayCommand ClearLogsCommand { get; }

        private void LoadConfiguration()
        {
            var config = _repository.Load(_configFilePath);
            BackupSets.Clear();

            foreach (var set in config.BackupSets)
            {
                BackupSets.Add(new BackupSetViewModel(set));
            }

            if (BackupSets.Count > 0)
            {
                SelectedBackupSet = BackupSets[0];
            }
            else
            {
                // Create a sample default backup set
                var sample = new BackupSetViewModel(new BackupSet
                {
                    Command = "sample_backup",
                    Remark = "Sample Daily Backup Set",
                    IsDirectory = true,
                    SourcePath = @"D:\Data\Current"
                });
                BackupSets.Add(sample);
                SelectedBackupSet = sample;
            }
        }

        private void ExecuteNewBackupSet()
        {
            int index = BackupSets.Count + 1;
            var newSet = new BackupSetViewModel(new BackupSet
            {
                Command = $"backup_{index}",
                Remark = $"New Backup Set #{index}",
                IsDirectory = true
            });

            BackupSets.Add(newSet);
            SelectedBackupSet = newSet;
            StatusMessage = $"Added new backup set: {newSet.Command}";
        }

        private void ExecuteDeleteBackupSet()
        {
            if (SelectedBackupSet == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete backup set '{SelectedBackupSet.Command}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var toRemove = SelectedBackupSet;
                int currentIndex = BackupSets.IndexOf(toRemove);
                BackupSets.Remove(toRemove);

                if (BackupSets.Count > 0)
                {
                    int nextIndex = Math.Clamp(currentIndex, 0, BackupSets.Count - 1);
                    SelectedBackupSet = BackupSets[nextIndex];
                }
                else
                {
                    SelectedBackupSet = null;
                }

                StatusMessage = $"Deleted backup set: {toRemove.Command}";
            }
        }

        public void ExecuteSaveAll()
        {
            var config = new BackupConfig
            {
                Version = "2.0",
                BackupSets = BackupSets.Select(b => b.ToModel()).ToList()
            };

            _repository.Save(config, _configFilePath);
            StatusMessage = $"Saved configuration ({config.BackupSets.Count} sets) at {DateTime.Now:HH:mm:ss}";
        }

        private void ExecuteAutoFillSlots()
        {
            if (SelectedBackupSet == null) return;

            if (string.IsNullOrWhiteSpace(ReferencePath))
            {
                MessageBox.Show("Please enter a Reference Path first (e.g. D:\\Backups\\Daily)", "Reference Path Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string baseRef = ReferencePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int count = Math.Clamp(SlotCountSelection, 1, 30);

            SelectedBackupSet.Slots.Clear();
            for (int i = 1; i <= count; i++)
            {
                string slotPath = SelectedBackupSet.IsDirectory
                    ? $"{baseRef}{Path.DirectorySeparatorChar}{i}"
                    : $"{baseRef}.{i}";

                SelectedBackupSet.Slots.Add(new RotationSlotItemViewModel(i, slotPath, false));
            }

            StatusMessage = $"Auto-filled {count} slots using reference: {baseRef}";
        }

        private void ExecuteCreateMissingFolders()
        {
            if (SelectedBackupSet == null) return;

            int created = 0;
            try
            {
                foreach (var slot in SelectedBackupSet.Slots)
                {
                    if (!string.IsNullOrWhiteSpace(slot.Path) && SelectedBackupSet.IsDirectory)
                    {
                        if (!Directory.Exists(slot.Path))
                        {
                            Directory.CreateDirectory(slot.Path);
                            created++;
                        }
                    }
                }

                MessageBox.Show($"Created {created} missing folder(s).", "Folders Created", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"Created {created} directory slot(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create directories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCopyCliSnippetCommand()
        {
            if (SelectedBackupSet == null) return;

            Clipboard.SetText(SelectedBackupSet.CliCommandSnippet);
            StatusMessage = "Copied Task Scheduler CLI command to clipboard!";
        }

        private async Task ExecuteTestRunBackupAsync()
        {
            if (SelectedBackupSet == null || IsRunningBackup) return;

            IsRunningBackup = true;
            StatusMessage = $"Running test for: {SelectedBackupSet.Command}...";
            ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Starting test execution for '{SelectedBackupSet.Command}'");

            var runner = new BackupRunner();
            var model = SelectedBackupSet.ToModel();

            try
            {
                bool success = await Task.Run(() => runner.RunAsync(model, log =>
                {
                    Application.Current.Dispatcher.Invoke(() => ExecutionLogs.Add(log));
                }));

                StatusMessage = success ? "Backup finished successfully" : "Backup finished with errors";
            }
            catch (Exception ex)
            {
                ExecutionLogs.Add($"[EXCEPTION] {ex.Message}");
                StatusMessage = "Backup failed with exception";
            }
            finally
            {
                IsRunningBackup = false;
            }
        }
    }
}
