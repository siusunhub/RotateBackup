using RotateBackup.Core.Models;

namespace RotateBackup.Wpf.ViewModels
{
    public class RotationSlotItemViewModel : ViewModelBase
    {
        private int _index;
        private string _path = string.Empty;
        private bool _keep;

        public RotationSlotItemViewModel(int index, string path = "", bool keep = false)
        {
            _index = index;
            _path = path;
            _keep = keep;
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public bool Keep
        {
            get => _keep;
            set => SetProperty(ref _keep, value);
        }

        public RotationSlot ToModel() => new RotationSlot
        {
            Index = Index,
            Path = Path,
            Keep = Keep
        };
    }
}
