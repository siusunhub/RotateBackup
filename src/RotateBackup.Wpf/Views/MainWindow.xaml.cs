using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private void BrowseLogPath_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedBackupSet != null)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Set Activity Log File Position",
                    Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".log",
                    FileName = !string.IsNullOrWhiteSpace(vm.SelectedBackupSet.LogPath)
                        ? System.IO.Path.GetFileName(vm.SelectedBackupSet.LogPath)
                        : $"{vm.SelectedBackupSet.Command}.log"
                };

                if (!string.IsNullOrWhiteSpace(vm.SelectedBackupSet.LogPath))
                {
                    string? dir = System.IO.Path.GetDirectoryName(vm.SelectedBackupSet.LogPath);
                    if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                    {
                        dialog.InitialDirectory = dir;
                    }
                }

                if (dialog.ShowDialog() == true)
                {
                    vm.SelectedBackupSet.LogPath = dialog.FileName;
                }
            }
        }

        private Point _dragStartPoint;
        private RotationSlotItemViewModel? _draggedSlot;

        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _draggedSlot = (sender as FrameworkElement)?.DataContext as RotationSlotItemViewModel;
        }

        private void DragHandle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedSlot != null)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is DependencyObject dragSource)
                    {
                        var data = new DataObject("RotationSlotItem", _draggedSlot);
                        DragDrop.DoDragDrop(dragSource, data, DragDropEffects.Move);
                        _draggedSlot = null;
                    }
                }
            }
        }

        private void SlotRow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("RotationSlotItem"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void SlotRow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("RotationSlotItem") && sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            }
        }

        private void SlotRow_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
            }
        }

        private void SlotRow_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
            }

            if (e.Data.GetDataPresent("RotationSlotItem"))
            {
                var droppedSlot = e.Data.GetData("RotationSlotItem") as RotationSlotItemViewModel;
                var targetSlot = (sender as FrameworkElement)?.DataContext as RotationSlotItemViewModel;
                if (droppedSlot != null && targetSlot != null && droppedSlot != targetSlot)
                {
                    if (DataContext is MainViewModel vm && vm.SelectedBackupSet != null)
                    {
                        var slots = vm.SelectedBackupSet.Slots;
                        int oldIndex = slots.IndexOf(droppedSlot);
                        int newIndex = slots.IndexOf(targetSlot);
                        if (oldIndex >= 0 && newIndex >= 0)
                        {
                            slots.Move(oldIndex, newIndex);
                            for (int i = 0; i < slots.Count; i++)
                            {
                                slots[i].Index = i + 1;
                            }
                            vm.StatusMessage = $"Reordered Slot #{oldIndex + 1} to Slot #{newIndex + 1}";
                        }
                    }
                }
            }
            e.Handled = true;
        }

        private void MoveSlotUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is RotationSlotItemViewModel slot)
            {
                MoveSlot(slot, -1);
            }
        }

        private void MoveSlotDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is RotationSlotItemViewModel slot)
            {
                MoveSlot(slot, 1);
            }
        }

        private void MoveSlot(RotationSlotItemViewModel slot, int direction)
        {
            if (DataContext is MainViewModel vm && vm.SelectedBackupSet != null)
            {
                var slots = vm.SelectedBackupSet.Slots;
                int currentIndex = slots.IndexOf(slot);
                if (currentIndex < 0) return;
                int targetIndex = currentIndex + direction;
                if (targetIndex < 0 || targetIndex >= slots.Count) return;

                slots.Move(currentIndex, targetIndex);
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].Index = i + 1;
                }
                vm.StatusMessage = $"Moved Slot #{currentIndex + 1} to Slot #{targetIndex + 1}";
            }
        }
    }
}
