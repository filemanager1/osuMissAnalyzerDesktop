using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class SettingsWindowViewModel : ViewModelBase
    {
        private string osuDir;
        private string osuDirError = "";
        private bool osuDirValid;
        private string songsDir;
        private string selectedTheme;
        private string apiKey;
        private bool watchdogMode;
        private bool minimizeToTray;
        private bool backgroundAnalysis = true;
        private bool backgroundNotifications = true;

        public Action<bool>? CloseAction { get; set; }
        public event EventHandler<string>? ErrorOccurred;

        public SettingsWindowViewModel(Options options)
        {
            osuDir = options.Settings.GetValueOrDefault("osudir", "");
            songsDir = options.Settings.GetValueOrDefault("songsdir", "");
            selectedTheme = options.Settings.GetValueOrDefault("colorscheme", "Default");
            apiKey = options.Settings.GetValueOrDefault("apikey", "");
            watchdogMode = options.WatchDogMode;
            minimizeToTray = options.MinimizeToTray;
            backgroundAnalysis = options.BackgroundAnalysis;
            backgroundNotifications = options.BackgroundNotifications;
        }

        // --- OsuDir ---

        public string OsuDir
        {
            get => osuDir;
            set
            {
                this.RaiseAndSetIfChanged(ref osuDir, value);
                ValidateOsuDir();
                this.RaisePropertyChanged(nameof(DefaultSongsDir));
            }
        }

        public string OsuDirError
        {
            get => osuDirError;
            set
            {
                this.RaiseAndSetIfChanged(ref osuDirError, value);
                this.RaisePropertyChanged(nameof(OsuDirErrorVisible));
            }
        }

        public bool OsuDirErrorVisible => !string.IsNullOrEmpty(OsuDirError);

        public bool OsuDirValid
        {
            get => osuDirValid;
            private set => this.RaiseAndSetIfChanged(ref osuDirValid, value);
        }

        private void ValidateOsuDir()
        {
            OsuDirValid = false;

            if (string.IsNullOrWhiteSpace(OsuDir))
            {
                OsuDirError = "";
                return;
            }

            if (!Directory.Exists(OsuDir))
            {
                OsuDirError = "Directory does not exist";
                return;
            }

            if (!File.Exists(Path.Combine(OsuDir, "osu!.db")))
            {
                OsuDirError = "osu!.db not found in this directory";
                return;
            }

            if (!File.Exists(Path.Combine(OsuDir, "scores.db")))
            {
                OsuDirError = "scores.db not found in this directory";
                return;
            }

            if (!Directory.Exists(Path.Combine(OsuDir, "Data", "r")))
            {
                OsuDirError = "Data/r/ directory not found";
                return;
            }

            OsuDirError = "";
            OsuDirValid = true;
        }

        // --- SongsDir ---

        public string DefaultSongsDir => string.IsNullOrEmpty(OsuDir) ? "" : Path.Combine(OsuDir, "Songs");

        public string SongsDir
        {
            get => songsDir;
            set => this.RaiseAndSetIfChanged(ref songsDir, value);
        }

        // --- Theme ---

        public string SelectedTheme
        {
            get => selectedTheme;
            set => this.RaiseAndSetIfChanged(ref selectedTheme, value);
        }

        public bool IsLightTheme
        {
            get => SelectedTheme == "Default";
            set
            {
                if (value) SelectedTheme = "Default";
            }
        }

        public bool IsDarkTheme
        {
            get => SelectedTheme == "Dark";
            set
            {
                if (value) SelectedTheme = "Dark";
            }
        }

        // --- API Key ---

        public string ApiKey
        {
            get => apiKey;
            set => this.RaiseAndSetIfChanged(ref apiKey, value);
        }

        // --- WatchDog ---

        public bool WatchDogMode
        {
            get => watchdogMode;
            set
            {
                this.RaiseAndSetIfChanged(ref watchdogMode, value);
                this.RaisePropertyChanged(nameof(BackgroundNotificationsEnabled));
            }
        }

        public bool MinimizeToTray
        {
            get => minimizeToTray;
            set
            {
                this.RaiseAndSetIfChanged(ref minimizeToTray, value);
                if (!value && backgroundNotifications)
                {
                    BackgroundNotifications = false;
                }
                this.RaisePropertyChanged(nameof(BackgroundNotificationsEnabled));
            }
        }

        public bool BackgroundAnalysis
        {
            get => backgroundAnalysis;
            set
            {
                this.RaiseAndSetIfChanged(ref backgroundAnalysis, value);
                if (!value && backgroundNotifications)
                {
                    BackgroundNotifications = false;
                }
                this.RaisePropertyChanged(nameof(BackgroundNotificationsEnabled));
            }
        }

        public bool BackgroundNotifications
        {
            get => backgroundNotifications;
            set => this.RaiseAndSetIfChanged(ref backgroundNotifications, value);
        }

        public bool BackgroundNotificationsEnabled => WatchDogMode && MinimizeToTray && BackgroundAnalysis;

        // --- Save ---

        public void Save()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    ["osudir"] = OsuDir,
                    ["watchdogmode"] = WatchDogMode ? "true" : "false",
                    ["minimizetotray"] = MinimizeToTray ? "true" : "false",
                    ["backgroundanalysis"] = BackgroundAnalysis ? "true" : "false",
                    ["backgroundnotifications"] = BackgroundNotifications ? "true" : "false",
                    ["colorscheme"] = SelectedTheme
                };

                if (!string.IsNullOrEmpty(SongsDir))
                    settings["songsdir"] = SongsDir;

                if (!string.IsNullOrEmpty(ApiKey))
                    settings["apikey"] = ApiKey;

                Options.WriteToFile("options.cfg", settings);
                CloseAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to save configuration:\n{ex.Message}");
            }
        }

        public void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}
