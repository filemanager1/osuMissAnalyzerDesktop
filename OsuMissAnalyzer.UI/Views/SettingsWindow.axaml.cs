using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OsuMissAnalyzer.UI.ViewModels;
using System;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs args)
        {
            if (DataContext is SettingsWindowViewModel vm)
            {
                vm.CloseAction = result => Close(result);
                vm.ErrorOccurred += OnError;
            }
        }

        private async void OnError(object? sender, string message)
        {
            await App.ShowMessageBox(message);
        }

        private void Save_Click(object? sender, RoutedEventArgs args)
        {
            if (DataContext is SettingsWindowViewModel vm)
            {
                vm.Save();
            }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs args)
        {
            Close(false);
        }

        private async void BrowseOsuDir(object? sender, RoutedEventArgs args)
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select osu! installation folder",
                AllowMultiple = false
            });
            if (result.Count > 0 && DataContext is SettingsWindowViewModel vm)
            {
                vm.OsuDir = result[0].Path.LocalPath;
            }
        }

        private async void BrowseSongsDir(object? sender, RoutedEventArgs args)
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Songs folder",
                AllowMultiple = false
            });
            if (result.Count > 0 && DataContext is SettingsWindowViewModel vm)
            {
                vm.SongsDir = result[0].Path.LocalPath;
            }
        }
    }
}
