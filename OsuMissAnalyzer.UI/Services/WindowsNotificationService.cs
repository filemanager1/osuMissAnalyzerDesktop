using System.Collections.Generic;
using System.IO;
using System.Linq;
using BMAPI.v1;
using BMAPI.v1.Events;
using DesktopNotifications;

namespace OsuMissAnalyzer.UI.Services;

public class PendingAnalysis
{
    public required string ReplayPath { get; init; }
    public required string BeatmapPath { get; init; }
}

public static class WindowsNotificationService
{
    public static INotificationManager? Manager { get; set; }
    public static PendingAnalysis? Pending { get; set; }

    public static string? GetBackgroundImagePath(Beatmap beatmap)
    {
        var bgEvent = beatmap.Events.OfType<ContentEvent>()
            .FirstOrDefault(e => e.Type == ContentType.Image);
        if (bgEvent == null || string.IsNullOrEmpty(bgEvent.Filename))
            return null;
        return Path.Combine(beatmap.Folder, bgEvent.Filename);
    }

    public static void ShowMissNotification(
        string beatmapTitle, string difficulty,
        int totalMisses, int misaim, int misclick, int notelock,
        string? backgroundImagePath = null)
    {
        if (Manager == null) return;
        if (totalMisses == 0) return;

        var parts = new List<string>();
        if (misaim > 0) parts.Add($"{misaim} misaim");
        if (misclick > 0) parts.Add($"{misclick} misclick");
        if (notelock > 0) parts.Add($"{notelock} notelock");
        string breakdown = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";

        var notification = new Notification
        {
            Title = $"{beatmapTitle} [{difficulty}]",
            Body = $"{totalMisses} miss(es){breakdown}",
        };

        if (backgroundImagePath != null && File.Exists(backgroundImagePath))
        {
            notification.BodyImagePath = backgroundImagePath;
        }

        notification.Buttons.Add(("View Misses", "view"));
        notification.Buttons.Add(("Dismiss", "dismiss"));

        Manager.ShowNotification(notification);
    }

    public static void ShowStatusNotification(string title, string body)
    {
        if (Manager == null) return;

        var notification = new Notification
        {
            Title = title,
            Body = body,
        };

        Manager.ShowNotification(notification);
    }
}
