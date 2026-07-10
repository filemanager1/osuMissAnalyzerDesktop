using System;
using System.IO;

namespace OsuMissAnalyzer.UI.Services;

public static class DebugLogger
{
    private static readonly string LogPath = "crash.log";

    public static void Log(Exception ex, string replayFile = "", string beatmapFile = "")
    {
        var entry = $"""
            ===== Crash at {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====
            Replay:  {replayFile}
            Beatmap: {beatmapFile}
            {ex}
            ========================================

            """;
        File.AppendAllText(LogPath, entry);
    }

    public static void LogInfo(string message)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";
        File.AppendAllText(LogPath, entry);
    }
}
