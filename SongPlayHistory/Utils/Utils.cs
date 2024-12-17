using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SongCore;
using SongPlayHistory.Model;
using SongPlayHistory.SongPlayData;

namespace SongPlayHistory.Utils
{
    internal static class Utils
    {
        #region replay check
        private static readonly Lazy<MethodBase?> ScoreSaber_playbackEnabled = new Lazy<MethodBase?>(() =>
        {
            var meta = Plugin.Instance.SSMetadata;
            if (meta == null)
            {
                Plugin.Log.Info("ScoreSaber is not installed or disabled");
                return null;
            }

            var method = meta.Assembly.GetType("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted")
                ?.GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                Plugin.Log.Warn("ScoreSaber replay check method not found");
                return null;
            }

            return method;
        });

        private static readonly Lazy<MethodBase?> GetBeatLeaderIsStartedAsReplay = new Lazy<MethodBase?>(() =>
        {
            var meta = Plugin.Instance.BLMetadata;
            if (meta == null)
            {
                Plugin.Log.Info("BeatLeader is not installed or disabled");
                return null;
            }

            var method = meta.Assembly.GetType("BeatLeader.Replayer.ReplayerLauncher")
                ?.GetProperty("IsStartedAsReplay", BindingFlags.Static | BindingFlags.Public)?.GetGetMethod(false);

            if (method == null)
            {
                Plugin.Log.Warn("BeatLeader ReplayerLauncher.IsStartedAsReplay not found");
                return null;
            }

            return method;
        });
        
        internal static bool IsInReplay()
        {
            var ssReplay = ScoreSaber_playbackEnabled.Value != null && (bool) ScoreSaber_playbackEnabled.Value.Invoke(null, null) == false;

            var blReplay = GetBeatLeaderIsStartedAsReplay.Value != null && (bool) GetBeatLeaderIsStartedAsReplay.Value.Invoke(null, null);
            
            return ssReplay || blReplay;
        }
        #endregion

        internal static string? GetLowerCaseCustomLevelHash(BeatmapLevel level)
        {
            var hash = Collections.GetCustomLevelHash(level.levelID).ToLower();
            return string.IsNullOrWhiteSpace(hash) ? null : hash;
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