using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using RotateBackup.Core.Models;
using RotateBackup.Core.Validation;

namespace RotateBackup.Wpf.ViewModels
{
    public class BackupSetViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        private string _id;
        private string _command;
        private string _remark;
        private bool _disabled;
        private bool _isDirectory;
        private string _sourcePath;
        private bool _keepSource;
        private bool _keepFirstDir;
        private string _preCommand;
        private string _postCommand;
        private bool _enableLog;
        private string _logPath;
        private bool _enableCrcCheck;

        public BackupSetViewModel(BackupSet? model = null)
        {
            model ??= new BackupSet();

            _id = model.Id;
            _command = model.Command;
            _remark = model.Remark;
            _disabled = model.Disabled;
            _isDirectory = model.IsDirectory;
            _sourcePath = model.SourcePath;
            _keepSource = model.KeepSource;
            _keepFirstDir = model.KeepFirstDir;
            _preCommand = model.PreCommand;
            _postCommand = model.PostCommand;
            _enableLog = model.EnableLog;
            _logPath = model.LogPath ?? string.Empty;
            _enableCrcCheck = model.EnableCrcCheck;

            Slots = new ObservableCollection<RotationSlotItemViewModel>();
            if (model.Slots != null && model.Slots.Count > 0)
            {
                foreach (var s in model.Slots.OrderBy(s => s.Index))
                {
                    Slots.Add(new RotationSlotItemViewModel(s.Index, s.Path, s.Keep));
                }
            }
            else
            {
                // Default 5 slots
                for (int i = 1; i <= 5; i++)
                {
                    Slots.Add(new RotationSlotItemViewModel(i));
                }
            }

            // The command name may come from an existing configuration file, so validate it
            // right away to show the problem immediately.
            ValidateCommandName();
        }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Command
        {
            get => _command;
            set
            {
                if (SetProperty(ref _command, value))
                {
                    OnPropertyChanged(nameof(CliCommandSnippet));
                    ValidateCommandName();
                }
            }
        }

        /// <summary>Validation message of the command name (empty when the name is valid).</summary>
        public string CommandNameError => CommandNameValidator.GetError(_command) ?? string.Empty;

        /// <summary>True when the command name contains invalid characters.</summary>
        public bool HasCommandNameError => CommandNameError.Length > 0;

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public bool Disabled
        {
            get => _disabled;
            set => SetProperty(ref _disabled, value);
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set
            {
                if (SetProperty(ref _isDirectory, value))
                {
                    OnPropertyChanged(nameof(IsFile));
                    OnPropertyChanged(nameof(TypeDisplayName));
                }
            }
        }

        public bool IsFile
        {
            get => !_isDirectory;
            set => IsDirectory = !value;
        }

        public string TypeDisplayName => IsDirectory ? "Directory" : "File";

        public string SourcePath
        {
            get => _sourcePath;
            set => SetProperty(ref _sourcePath, value);
        }

        public bool KeepSource
        {
            get => _keepSource;
            set => SetProperty(ref _keepSource, value);
        }

        public bool KeepFirstDir
        {
            get => _keepFirstDir;
            set => SetProperty(ref _keepFirstDir, value);
        }

        public string PreCommand
        {
            get => _preCommand;
            set => SetProperty(ref _preCommand, value);
        }

        public string PostCommand
        {
            get => _postCommand;
            set => SetProperty(ref _postCommand, value);
        }

        public bool EnableLog
        {
            get => _enableLog;
            set
            {
                if (SetProperty(ref _enableLog, value))
                {
                    if (value && string.IsNullOrWhiteSpace(_logPath))
                    {
                        string safeName = !string.IsNullOrWhiteSpace(Command) ? Command : "backup";
                        LogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"{safeName}.log");
                    }
                }
            }
        }

        public string LogPath
        {
            get => _logPath;
            set => SetProperty(ref _logPath, value);
        }

        /// <summary>
        /// When enabled, every copied file is re-read from disk and its CRC-32 checksum is
        /// compared with the checksum of the transferred bytes before it is marked as verified.
        /// </summary>
        public bool EnableCrcCheck
        {
            get => _enableCrcCheck;
            set => SetProperty(ref _enableCrcCheck, value);
        }

        public ObservableCollection<RotationSlotItemViewModel> Slots { get; }

        public string CliCommandSnippet
        {
            get
            {
                string exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RotateBackup.exe");
                return $"\"{exePath}\" {Command}";
            }
        }

        public BackupSet ToModel()
        {
            return new BackupSet
            {
                Id = Id,
                Command = Command,
                Remark = Remark,
                Disabled = Disabled,
                IsDirectory = IsDirectory,
                SourcePath = SourcePath,
                KeepSource = KeepSource,
                KeepFirstDir = KeepFirstDir,
                PreCommand = PreCommand,
                PostCommand = PostCommand,
                EnableLog = EnableLog,
                LogPath = LogPath,
                EnableCrcCheck = EnableCrcCheck,
                Slots = Slots.Select(s => s.ToModel()).ToList()
            };
        }

        #region Validation

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.SelectMany(e => e.Value);
            }

            return _errors.TryGetValue(propertyName, out List<string>? messages)
                ? messages
                : Array.Empty<string>();
        }

        /// <summary>Validates the command name and reports the result to the binding engine.</summary>
        private void ValidateCommandName()
        {
            string? error = CommandNameValidator.GetError(_command);

            if (error == null)
            {
                _errors.Remove(nameof(Command));
            }
            else
            {
                _errors[nameof(Command)] = new List<string> { error };
            }

            OnPropertyChanged(nameof(CommandNameError));
            OnPropertyChanged(nameof(HasCommandNameError));

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Command)));
        }

        #endregion
    }
}
