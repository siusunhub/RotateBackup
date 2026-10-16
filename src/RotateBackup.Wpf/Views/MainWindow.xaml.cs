using System.Windows;
using Microsoft.Win32;
using RotateBackup.Wpf.ViewModels;

namespace RotateBackup.Wpf.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseSourcePath_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedBackupSet != null)
            {
                if (vm.SelectedBackupSet.IsDirectory)
                {
                    var dialog = new OpenFolderDialog
                    {
                        Title = "Select Source Folder to Backup"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        vm.SelectedBackupSet.SourcePath = dialog.FolderName;
                    }
                }
                else
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = "Select Source File to Backup"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        vm.SelectedBackupSet.SourcePath = dialog.FileName;
                    }
                }
            }
        }

        private void BrowseSlotPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RotationSlotItemViewModel slot)
            {
                if (DataContext is MainViewModel vm && vm.SelectedBackupSet != null)
                {
                    if (vm.SelectedBackupSet.IsDirectory)
                    {
                        var dialog = new OpenFolderDialog
                        {
                            Title = $"Select Destination Folder for Slot #{slot.Index}"
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            slot.Path = dialog.FolderName;
                        }
                    }
                    else
                    {
                        var dialog = new SaveFileDialog
                        {
                            Title = $"Select Target File for Slot #{slot.Index}"
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            slot.Path = dialog.FileName;
                        }
                    }
                }
            }
        }
    }
}
