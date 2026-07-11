using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using OsuDbAPI;
using OsuMissAnalyzer.UI.ViewModels;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class BeatmapSearchBox : Window
    {
        public Beatmap Result => (DataContext as BeatmapSearchBoxViewModel)!.Result;

        public BeatmapSearchBox()
        {
            InitializeComponent();
            SearchBox.KeyDown += SearchBox_KeyDown;
        }

        public void ButtonClicked(object? sender, RoutedEventArgs args)
        {
            if (sender is Button b && (b.Content as string) == "Ok")
                Close(Result != null);
            else
                Close(false);
        }

        public void Text_DoubleTapped(object o, RoutedEventArgs e)
        {
            Close(Result != null);
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                (DataContext as BeatmapSearchBoxViewModel)!.StartSearch(true);
            }
        }
    }
}
