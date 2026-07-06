using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class SetupWizardViewModel : ViewModelBase
    {
        private int currentPage;
        private string osuDir = "";
        private string osuDirError = "";
        private bool osuDirValid;
        private string songsDir = "";
        private string selectedTheme;
        private string apiKey = "";
        private bool watchdogMode;
        private bool isCompleted;

        public Action<bool>? CloseAction { get; set; }
        public event EventHandler<string>? ErrorOccurred;

        public SetupWizardViewModel(Options options)
        {
            selectedTheme = options.Settings.GetValueOrDefault("colorscheme", "Default");
        }

        // --- Page Navigation ---

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                this.RaiseAndSetIfChanged(ref currentPage, value);
                this.RaisePropertyChanged(nameof(Page1Visible));
                this.RaisePropertyChanged(nameof(Page2Visible));
                this.RaisePropertyChanged(nameof(Page3Visible));
                this.RaisePropertyChanged(nameof(Page4Visible));
                this.RaisePropertyChanged(nameof(Page5Visible));
                this.RaisePropertyChanged(nameof(CanGoBack));
                this.RaisePropertyChanged(nameof(CanGoNext));
                this.RaisePropertyChanged(nameof(IsLastPage));
                this.RaisePropertyChanged(nameof(IsNotLastPage));
                this.RaisePropertyChanged(nameof(PageIndicator));
                this.RaisePropertyChanged(nameof(NextButtonText));
            }
        }

        public bool Page1Visible => CurrentPage == 0;
        public bool Page2Visible => CurrentPage == 1;
        public bool Page3Visible => CurrentPage == 2;
        public bool Page4Visible => CurrentPage == 3;
        public bool Page5Visible => CurrentPage == 4;
        public bool CanGoBack => CurrentPage > 0;
        public bool CanGoNext => CurrentPage < 4 && (CurrentPage != 0 || OsuDirValid);
        public bool IsLastPage => CurrentPage == 4;
        public bool IsNotLastPage => CurrentPage < 4;
        public string PageIndicator => $"Step {CurrentPage + 1} of 5";
        public string NextButtonText => IsLastPage ? "Finish" : "Next >";

        // --- Page 1: OsuDir ---

        public string OsuDir
        {
            get => osuDir;
            set
            {
                this.RaiseAndSetIfChanged(ref osuDir, value);
                ValidateOsuDir();
                this.RaisePropertyChanged(nameof(DefaultSongsDir));
                this.RaisePropertyChanged(nameof(SummaryText));
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
            private set
            {
                this.RaiseAndSetIfChanged(ref osuDirValid, value);
                this.RaisePropertyChanged(nameof(CanGoNext));
            }
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

        // --- Page 2: SongsDir ---

        public string DefaultSongsDir => string.IsNullOrEmpty(OsuDir) ? "" : Path.Combine(OsuDir, "Songs");

        public string SongsDir
        {
            get => songsDir;
            set
            {
                this.RaiseAndSetIfChanged(ref songsDir, value);
                this.RaisePropertyChanged(nameof(SummaryText));
            }
        }

        // --- Page 3: Theme ---

        public string SelectedTheme
        {
            get => selectedTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref selectedTheme, value);
                this.RaisePropertyChanged(nameof(SummaryText));
            }
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

        // --- Page 4: API Key ---

        public string ApiKey
        {
            get => apiKey;
            set
            {
                this.RaiseAndSetIfChanged(ref apiKey, value);
                this.RaisePropertyChanged(nameof(SummaryText));
            }
        }

        // --- Page 5: WatchDog + Summary ---

        public bool WatchDogMode
        {
            get => watchdogMode;
            set
            {
                this.RaiseAndSetIfChanged(ref watchdogMode, value);
                this.RaisePropertyChanged(nameof(SummaryText));
            }
        }

        public bool IsCompleted
        {
            get => isCompleted;
            set => this.RaiseAndSetIfChanged(ref isCompleted, value);
        }

        public string SummaryText
        {
            get
            {
                string songs = !string.IsNullOrEmpty(SongsDir) ? SongsDir : DefaultSongsDir + " (default)";
                string theme = SelectedTheme == "Dark" ? "Dark" : "Light";
                string api = !string.IsNullOrEmpty(ApiKey) ? "Configured" : "Not configured";
                string wd = WatchDogMode ? "Enabled" : "Disabled";
                return $"osu! Directory: {OsuDir}\nSongs Directory: {songs}\nTheme: {theme}\nAPI Key: {api}\nWatchDog Mode: {wd}";
            }
        }

        // --- Commands ---

        public void Next()
        {
            if (CanGoNext)
                CurrentPage++;
        }

        public void Back()
        {
            if (CanGoBack)
                CurrentPage--;
        }

        public void Finish()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    ["osudir"] = OsuDir,
                    ["watchdogmode"] = WatchDogMode ? "true" : "false",
                    ["colorscheme"] = SelectedTheme
                };

                if (!string.IsNullOrEmpty(SongsDir))
                    settings["songsdir"] = SongsDir;

                if (!string.IsNullOrEmpty(ApiKey))
                    settings["apikey"] = ApiKey;

                Options.WriteToFile("options.cfg", settings);
                IsCompleted = true;
                CloseAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to save configuration:\n{ex.Message}");
            }
        }
    }
}
