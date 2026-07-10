using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BMAPI.v1;
using OsuDbAPI;
using OsuMissAnalyzer.Core;
using ReplayAPI;

namespace OsuMissAnalyzer.UI
{
	public class Options
	{
		public Dictionary<string, string> Settings { get; private set; }
		public OsuDbFile Database;
		public ScoresDb ScoresDb;
		public bool OsuDirAccessible { get; private set; }
        public string SongsFolder => Settings.GetValueOrDefault("songsdir", Settings.ContainsKey("osudir") ? Path.Combine(Settings["osudir"], "Songs") : null);
        public bool WatchDogMode => "true" == Settings.GetValueOrDefault("watchdogmode", Settings.ContainsKey("watchdogmode") ? Settings["watchdogmode"].ToLowerInvariant() : "false");
        public bool MinimizeToTray => "true" == Settings.GetValueOrDefault("minimizetotray", Settings.ContainsKey("minimizetotray") ? Settings["minimizetotray"].ToLowerInvariant() : "false");
        public bool BackgroundAnalysis => "true" == Settings.GetValueOrDefault("backgroundanalysis", Settings.ContainsKey("backgroundanalysis") ? Settings["backgroundanalysis"].ToLowerInvariant() : "true");
        public bool BackgroundNotifications => "true" == Settings.GetValueOrDefault("backgroundnotifications", Settings.ContainsKey("backgroundnotifications") ? Settings["backgroundnotifications"].ToLowerInvariant() : "true");
		public static void WriteToFile(string file, Dictionary<string, string> settings)
		{
			using var writer = new StreamWriter(file);
			foreach (var kv in settings)
				if (!string.IsNullOrEmpty(kv.Value))
					writer.WriteLine($"{kv.Key}={kv.Value}");
		}
		public Options(string file, Dictionary<string, string> optList)
		{
			OsuDirAccessible = false;
			Settings = new Dictionary<string, string>();
			using (StreamReader f = new StreamReader(file))
			{
				while (!f.EndOfStream)
				{
					string[] s = f.ReadLine().Trim().Split(new char[] { '=' }, 2);
					AddOption(s[0].ToLower(), s[1].Trim());
				}
			}
			foreach(var kv in optList)
			{
				AddOption(kv.Key, kv.Value);
			}
		}
		public BMAPI.v1.Beatmap GetBeatmapFromHash(string mapHash)
		{
			var entry = Database.GetBeatmapFromHash(mapHash);
			if (entry == null) return null;

			var path = Path.Combine(SongsFolder, entry.FolderName, entry.OsuFile);
			if (File.Exists(path))
				return new BMAPI.v1.Beatmap(path);

			// osu!.db FolderName might be truncated for long folder names.
			// Fall back to searching by set ID prefix.
			if (entry.SetID > 0)
			{
				var prefix = entry.SetID + " ";
				var folder = Directory.GetDirectories(SongsFolder, prefix + "*").FirstOrDefault();
				if (folder != null)
				{
					var altPath = Path.Combine(folder, entry.OsuFile);
					if (File.Exists(altPath))
						return new BMAPI.v1.Beatmap(altPath);
				}
			}

			return null;
		}
		public BMAPI.v1.Beatmap GetBeatmapFromId(int mapId)
        {
			return Database.GetBeatmapFromId(mapId)?.Load(SongsFolder);
        }
		public List<Replay> GetReplaysFromBeatmap(string beatmapHash)
		{
			return ScoresDb.scores.GetValueOrDefault(beatmapHash, new Score[] { }).Select(s => new Replay(Path.Combine(Settings["osudir"], "Data", "r", s.filename))).ToList();
		}
		private void AddOption(string key, string value)
		{
			if (value.Length > 0)
			{
				Settings.Add(key, value);
				if (key == "osudir"
					&& File.Exists(Path.Combine(value, "osu!.db"))
					&& File.Exists(Path.Combine(value, "scores.db"))
					&& Directory.Exists(Path.Combine(value, "Data", "r")))
				{
					Database = new OsuDbFile(Path.Combine(value, "osu!.db"), byHash: true);
					ScoresDb = new ScoresDb(Path.Combine(value, "scores.db"));
					OsuDirAccessible = true;
				}
			}
		}
	}
}
