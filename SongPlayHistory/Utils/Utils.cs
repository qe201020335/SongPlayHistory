using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using SongCore.Utilities;
using SongPlayHistory.Model;
using SongPlayHistory.SongPlayData;

namespace SongPlayHistory.Utils
{
    internal static class Utils
    {
        #region replay check (copied from HRCounter) 
        // MIT LICENSE: https://github.com/qe201020335/HRCounter/blob/master/LICENSE)
        // copied from Camera2
        private static readonly MethodBase? ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");

        private static readonly MethodBase? GetBeatLeaderIsStartedAsReplay =
            AccessTools.Property(AccessTools.TypeByName("BeatLeader.Replayer.ReplayerLauncher"), "IsStartedAsReplay")?.GetGetMethod(false);

        
        internal static bool IsInReplay()
        {
            // copied from Camera2
            var ssReplay = ScoreSaber_playbackEnabled != null && (bool) ScoreSaber_playbackEnabled.Invoke(null, null) == false;

            var blReplay = GetBeatLeaderIsStartedAsReplay != null && (bool) GetBeatLeaderIsStartedAsReplay.Invoke(null, null);
            
            return ssReplay || blReplay;
        }
        #endregion

        internal static string? GetCustomLevelHash(BeatmapLevel level)
        {
            return level.levelID.StartsWith("custom_level_") ? Hashing.GetCustomLevelHash(level) : null;
        }
        
        internal static IList<ISongPlayRecord> Copy(this IEnumerable<Record> records)
        {
            return records.Select(record => record.Copy()).ToList();
        }
        
        internal static StringBuilder TMPSpace(this StringBuilder s, int len)
        {
            var space = string.Concat(Enumerable.Repeat("_", len));
            return s.Append($"<size=1><color=#00000000>{space}</color></size>");
        }
    }
}