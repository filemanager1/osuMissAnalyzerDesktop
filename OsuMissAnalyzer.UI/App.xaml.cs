using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using Avalonia.Threading;
using BMAPI.v1;
using DesktopNotifications;
using DesktopNotifications.Windows;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.UI.Services;
using OsuMissAnalyzer.UI.ViewModels;
using OsuMissAnalyzer.UI.Views;
using ReactiveUI;
using ReplayAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI
{
    public class App : Application
    {
        public static Window Window { get; private set; }
        public static UIReplayLoader ReplayLoader { get; private set; }

        public static bool IsWindowInBackground { get; set; }
        internal static bool PendingForegroundReplay { get; set; }

        private static string? _lastReplayPath;
        private static DateTime _lastReplayTime = DateTime.MinValue;
        private TrayIcon? _trayIcon;

        public App() {}
        public App(UIReplayLoader replayLoader)
        {
            ReplayLoader = replayLoader;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            RequestedThemeVariant = ReplayLoader.ColorScheme.SchemeType == Core.ColorScheme.Type.Dark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Window = desktop.MainWindow = new MissWindow();
                Window.Show();

                if (!ReplayLoader.Options.OsuDirAccessible && !Program.headless)
                {
                    var vm = new SetupWizardViewModel(ReplayLoader.Options);
                    var wizard = new SetupWizardWindow { DataContext = vm };
                    bool completed = await wizard.ShowDialog<bool>(Window);
                    if (!completed)
                    {
                        Window.Close();
                        return;
                    }
                    ReplayLoader.Options = new Options("options.cfg", new Dictionary<string, string>());
                    ApplyTheme();
                }

                if (ReplayLoader.Options.WatchDogMode && (!ReplayLoader.Options.Settings.ContainsKey("osudir") || string.IsNullOrEmpty(ReplayLoader.Options.Settings["osudir"])))
                {
                    await ShowMessageBox("OsuDir is required when WatchDogMode is enabled.");
                    Window.Close();
                    return;
                }

                await Load(ReplayLoader);
                if (ReplayLoader.Options.WatchDogMode)
                {
                    InitNotifications();
                    ReplayLoader.NewReplay += ReplayLoaderOnNewReplay;
                    ReplayLoader.WatchForNewReplays();

                    if (ReplayLoader.Options.MinimizeToTray)
                    {
                        SetupTrayIcon();
                    }
                }

                desktop.Exit += (s, e) =>
                {
                    WindowsNotificationService.Manager?.Dispose();
                    _trayIcon?.Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void InitNotifications()
        {
            try
            {
                var context = WindowsApplicationContext.FromCurrentProcess("OsuMissAnalyzer");
                var manager = new WindowsNotificationManager(context);
                WindowsNotificationService.Manager = manager;
                manager.Initialize().GetAwaiter().GetResult();
                manager.NotificationActivated += OnNotificationActivated;
            }
            catch (Exception)
            {
            }
        }

        private void SetupTrayIcon()
        {
            using var iconStream = AssetLoader.Open(new Uri("avares://OsuMissAnalyzer/Assets/logo.ico"));
            var icon = new WindowIcon(iconStream);
            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "osuMissAnalyzer (WatchDog active)",
                Menu = new NativeMenu(),
            };

            _trayIcon.Clicked += (s, e) => ShowMainWindow();

            var showItem = new NativeMenuItem("Show Window")
            {
                Command = ReactiveCommand.Create(ShowMainWindow)
            };
            var exitItem = new NativeMenuItem("Exit")
            {
                Command = ReactiveCommand.Create(() =>
                {
                    _trayIcon?.Dispose();
                    Environment.Exit(0);
                })
            };

            _trayIcon.Menu.Items.Add(showItem);
            _trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
            _trayIcon.Menu.Items.Add(exitItem);

            _trayIcon.IsVisible = false;

            Window.GetObservable(Avalonia.Controls.Window.WindowStateProperty).Subscribe(state =>
            {
                if (_trayIcon == null) return;

                if (state == WindowState.Minimized)
                {
                    _trayIcon.IsVisible = true;
                    Window.Hide();
                    if (WindowsNotificationService.Manager != null)
                    {
                        WindowsNotificationService.ShowStatusNotification(
                            "osuMissAnalyzer",
                            "WatchDog active, monitoring replays in background!");
                    }
                }
                else
                {
                    _trayIcon.IsVisible = false;
                }
            });
        }

        private void ShowMainWindow()
        {
            Window.Show();
            Window.WindowState = WindowState.Normal;
            Window.Activate();
            if (_trayIcon != null) _trayIcon.IsVisible = false;
        }

        private void ReplayLoaderOnNewReplay(object? sender, EventArgs e)
        {
            if (!ReplayLoader.Options.BackgroundAnalysis && IsWindowInBackground)
            {
                PendingForegroundReplay = true;
                return;
            }

            var osuDir = ReplayLoader.Options.Settings.GetValueOrDefault("osudir", "");
            var latestPath = GetLatestReplayPath(osuDir);
            if (latestPath == _lastReplayPath && (DateTime.Now - _lastReplayTime).TotalSeconds < 5)
            {
                return;
            }

            _lastReplayPath = latestPath;
            _lastReplayTime = DateTime.Now;

            if (latestPath == null)
            {
                return;
            }

            if (IsWindowInBackground)
            {
                _ = HandleBackgroundReplaySafe(latestPath);
            }
            else
            {
                _ = Load(ReplayLoader);
            }
        }

        private async Task HandleBackgroundReplaySafe(string path)
        {
            try
            {
                await HandleBackgroundReplay(path);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(ex, path);
            }
        }

private async Task HandleBackgroundReplay(string path)
{
    await Task.Delay(1000);
    if (!File.Exists(path))
    {
        return;
    }

    Replay? replay = await Task.Run(() =>
    {
        try
        {
            return new Replay(path);
        }
        catch (Exception)
        {
            return null;
        }
    });
    if (replay?.GameMode != GameModes.osu)
    {
        return;
    }

    var tempLoader = new UIReplayLoader { Options = ReplayLoader.Options };
    Beatmap? beatmap = await tempLoader.LoadBeatmap(replay, dialog: false);
    if (beatmap == null)
    {
        return;
    }

    var analyzer = new ReplayAnalyzer(beatmap, replay);
    if (analyzer.misses.Count == 0)
    {
        return;
    }

    int misaim = 0, misclick = 0, notelock = 0;
    foreach (var miss in analyzer.misses)
    {
        switch (MissClassifier.Classify(miss, replay, analyzer))
        {
            case MissVerdict.Misaim:   misaim++;   break;
            case MissVerdict.Misclick: misclick++; break;
            case MissVerdict.Notelock: notelock++; break;
        }
    }

    WindowsNotificationService.Pending = new PendingAnalysis
    {
        ReplayPath = path,
        BeatmapPath = beatmap.Filename,
    };

    if (ReplayLoader.Options.BackgroundNotifications)
    {
        string? backgroundImagePath = WindowsNotificationService.GetBackgroundImagePath(beatmap);

        WindowsNotificationService.ShowMissNotification(
            $"{beatmap.Artist} - {beatmap.Title}",
            beatmap.Version,
            analyzer.misses.Count, misaim, misclick, notelock,
            backgroundImagePath);
    }

    PendingForegroundReplay = true;
}

        private void OnNotificationActivated(object? sender, NotificationActivatedEventArgs e)
        {
            if (e.ActionId != "view" || WindowsNotificationService.Pending == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                ShowMainWindow();

                var loader = new UIReplayLoader
                {
                    Options = ReplayLoader.Options,
                    ReplayFile = WindowsNotificationService.Pending.ReplayPath,
                    BeatmapFile = WindowsNotificationService.Pending.BeatmapPath,
                };
                _ = Load(loader);
            });
        }

        private static string? GetLatestReplayPath(string osuDir)
        {
            string osuReplays = Path.Combine(osuDir, "Data", "r");
            string userReplays = Path.Combine(osuDir, "Replays");

            var files = new List<string>();
            if (Directory.Exists(osuReplays))
                files.AddRange(Directory.GetFiles(osuReplays, "*.osr"));
            if (Directory.Exists(userReplays))
                files.AddRange(Directory.GetFiles(userReplays, "*.osr"));

            return files
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();
        }

        public static async Task Load(UIReplayLoader loader)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => Load(loader));
                return;
            }
            string? errorMessage, result = null;
            do
            {
                try
                {
                    if ((errorMessage = await loader.Load()) == null)
                    {
                        Window.DataContext = new MissWindowViewModel(loader);
                        return;
                    }

                    if (loader.Options.WatchDogMode)
                        return;
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    File.WriteAllText("exception.log", e.ToString());
                }
                if (errorMessage != null)
                {
                    result = await ShowMessageBox($"An error has occurred.\n{errorMessage}", "OK", "Reload");
                    if (result != "Reload")
                    {
                        Window.Close();
                        return;
                    }
                    else
                    {
                        loader = new UIReplayLoader { Options = loader.Options };
                    }
                }
            } while (result == "Reload");
        }

        public static async Task ShowMessageBox(string message)
        {
            await ShowMessageBox(message, "OK");
        }

        public static async Task<string> ShowMessageBox(string message, params string[] buttons)
        {
            var window = new MessageBox
            {
                DataContext = new MessageBoxViewModel
                {
                    Message = message,
                    Options = buttons
                },
            };
            return await window.ShowDialog<string>(Window);
        }
    }
}
