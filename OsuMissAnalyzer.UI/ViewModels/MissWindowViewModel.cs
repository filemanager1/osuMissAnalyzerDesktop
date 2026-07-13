using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using OsuMissAnalyzer.Core;
using ReactiveUI;
using System.Reactive.Linq;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class MissWindowViewModel : ViewModelBase
    {

        private MissAnalyzer analyzer;
        public MissAnalyzer Analyzer { get => analyzer; set => this.RaiseAndSetIfChanged(ref analyzer, value); }

        public Func<Image, Task>? ClipboardCopyHandler { get; set; }

        private UIReplayLoader loader;
        public UIReplayLoader Loader { get => loader; set => this.RaiseAndSetIfChanged(ref loader, value); }

        private Image image;
        public Image Image { get => image; set => this.RaiseAndSetIfChanged(ref image, value); }

        private Rect bounds;
        public Rect Bounds { get => bounds; set => this.RaiseAndSetIfChanged(ref bounds, value); }

        public Rectangle Area => new Rectangle(0, 0, (int)Bounds.Width, (int)Bounds.Height);

        public MissWindowViewModel(UIReplayLoader loader)
        {
            Loader = loader;
            Analyzer = new MissAnalyzer(loader);
            this.WhenAnyValue(x => x.Bounds, x => x.Analyzer).Subscribe(((Rect, MissAnalyzer) _) => UpdateImage());
        }

        internal void OnMouseReleased(object? sender, PointerReleasedEventArgs e)
        {
            switch (e.InitialPressMouseButton)
            {
                case MouseButton.Left:
                    Analyzer.NextObject();
                    break;
                case MouseButton.Right:
                    Analyzer.PreviousObject();
                    break;
            }
            UpdateImage();
        }

        internal async void OnKeyDown(object? source, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Up:
                        Analyzer.ScaleChange(-1);
                        break;
                    case Key.Down:
                        Analyzer.ScaleChange(1);
                        break;
                    case Key.Right:
                        Analyzer.NextObject();
                        break;
                    case Key.Left:
                        Analyzer.PreviousObject();
                        break;
                    case Key.T:
                        Analyzer.ToggleOutlines();
                        break;
                    case Key.P:
                        int i = 0;
                        foreach (var img in Analyzer.DrawAllMisses(Area))
                        {
                            string filename = $"{Path.GetFileNameWithoutExtension(Loader.Replay!.Filename)}.{i++}.png";
                            await img.SaveAsPngAsync(filename);
                        }
                        break;
                    case Key.R:
                        _ = App.Load(new UIReplayLoader { Options = Loader.Options });
                        break;
                    case Key.O:
                        if (e.KeyModifiers == KeyModifiers.Control)
                            _ = App.Load(new UIReplayLoader { Options = Loader.Options, ShowReplayPicker = true });
                        break;
                    case Key.OemComma:
                        if (e.KeyModifiers == KeyModifiers.Control)
                            App.OpenSettings(Loader.Options);
                        break;
                    case Key.A:
                        Analyzer.ToggleDrawAllHitObjects();
                        break;
                    case Key.C:
                        if (ClipboardCopyHandler != null && Image != null)
                            await ClipboardCopyHandler(Image);
                        return;
                }
                UpdateImage();
            }
            catch (Exception ex)
            {
                File.WriteAllText("exception.log", $"OnKeyDown: {ex}");
            }
        }
        internal void OnMouseWheel(object? source, PointerWheelEventArgs e)
        {
            Analyzer.ScaleChange(-Math.Sign(e.Delta.Y));
            UpdateImage();
        }

        internal void UpdateImage()
        {
            if (Area.Width > 0 && Area.Height > 0)
            {
                Image = Analyzer.DrawSelectedHitObject(Area);
            }
        }
    }
}