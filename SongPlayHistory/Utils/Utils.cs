using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
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

        internal static bool TryGetHashFromLevelId(string levelId, out string hash)
        {
            if (levelId.StartsWith("custom_level_") && !levelId.Contains("WIP"))
            {
                // When there are duplicated levels
                // the level id would be custom_level_HASHHASHHASH_SONGFOLDERNAME
                var id = levelId.Replace("custom_level_", "");
                var index = id.IndexOf("_");
                if (index >= 0 && id.Length >= index)
                {
                    id = id.Substring(0, index);
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    hash = id;
                    return true;
                }
            }
            
            hash = "";
            return false;
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