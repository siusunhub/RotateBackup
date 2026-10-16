using System;
using System.Collections.ObjectModel;
using System.Linq;
using RotateBackup.Core.Models;

namespace RotateBackup.Wpf.ViewModels
{
    public class BackupSetViewModel : ViewModelBase
    {
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
                }
            }
        }

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
                Slots = Slots.Select(s => s.ToModel()).ToList()
            };
        }
    }
}
