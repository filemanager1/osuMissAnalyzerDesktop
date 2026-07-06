using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OsuMissAnalyzer.UI.ViewModels;
using System;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class SetupWizardWindow : Window
    {
        public SetupWizardWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs args)
        {
            if (DataContext is SetupWizardViewModel vm)
            {
                vm.CloseAction = result => Close(result);
                vm.ErrorOccurred += OnError;
            }
        }

        private async void OnError(object? sender, string message)
        {
            await App.ShowMessageBox(message);
        }

        private async void BrowseOsuDir(object? sender, RoutedEventArgs args)
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select osu! installation folder",
                AllowMultiple = false
            });
            if (result.Count > 0 && DataContext is SetupWizardViewModel vm)
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
            if (result.Count > 0 && DataContext is SetupWizardViewModel vm)
            {
                vm.SongsDir = result[0].Path.LocalPath;
            }
        }
    }
}
