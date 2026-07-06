using System;
using BMAPI.v1.HitObjects;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core.Utils;
using ReplayAPI;

namespace OsuMissAnalyzer.UI.Services;

public enum MissVerdict { Misaim, Misclick, Notelock }

public static class MissClassifier
{
    public static MissVerdict Classify(CircleObject hitObject, Replay replay, ReplayAnalyzer analyzer)
    {
        float od = hitObject.Beatmap.OverallDifficulty;
        var frames = replay.ReplayFrames;

        bool cursorOnCircle = false;
        float closestKeyTime = float.MaxValue;
        float closestDistance = 0f;

        for (int k = 0; k < frames.Count - 1; k++)
        {
            float t0 = frames[k].Time;
            float t1 = frames[k + 1].Time;

            if (t0 <= hitObject.StartTime && t1 > hitObject.StartTime)
            {
                cursorOnCircle = hitObject.ContainsPoint(frames[k].GetPoint2());
            }

            Keys prevKeys = k == 0 ? Keys.None : frames[k - 1].Keys;
            if (analyzer.getKey(prevKeys, frames[k].Keys) > 0)
            {
                float hitAcc = frames[k].Time - hitObject.StartTime;
                float hw50 = 199.5f - 10 * od;
                if (Math.Abs(hitAcc) < hw50 && Math.Abs(hitAcc) < Math.Abs(closestKeyTime))
                {
                    closestKeyTime = hitAcc;
                    closestDistance = hitObject.DistanceToPoint(frames[k].GetPoint2());
                }
            }
        }

        if (closestDistance <= 0) return MissVerdict.Notelock;
        if (cursorOnCircle) return MissVerdict.Misclick;
        return MissVerdict.Misaim;
    }
}
