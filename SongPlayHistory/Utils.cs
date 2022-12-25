using System.Linq;
using System.Reflection;
using BeatLeader.Replayer;
using HarmonyLib;
using IPA.Loader;
using Hive.Versioning;

namespace SongPlayHistoryContinued
{
    internal static class Utils
    {
        #region replay check (copied from HRCounter, MIT LICENSE: https://github.com/qe201020335/HRCounter/blob/master/LICENSE)
        private const string BEATLEADER_MOD_ID = "BeatLeader";

        private static bool? _beatleaderHasReplay = null;

        internal static bool BeatLeaderHasReplay
        {
            get
            {
                var blVersion = FindEnabledPluginMetadata(BEATLEADER_MOD_ID)?.HVersion;
                _beatleaderHasReplay ??= blVersion != null && blVersion >= new Version(0, 5, 0);
                return _beatleaderHasReplay.Value;
            }
        }

        // copied from Camera2
        private static readonly MethodBase? ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");
        
        internal static bool IsInReplay()
        {
            // copied from Camera2
            var ssReplay = ScoreSaber_playbackEnabled != null && (bool) ScoreSaber_playbackEnabled.Invoke(null, null) == false;

            var blReplay = BeatLeaderHasReplay && ReplayerLauncher.IsStartedAsReplay;
            
            return ssReplay || blReplay;
        }
        #endregion replay
        
        internal static bool IsModEnabled(string id)
        {
            return FindEnabledPluginMetadata(id) != null;
        }
        
        internal static PluginMetadata? FindEnabledPluginMetadata(string id)
        {
            return PluginManager.EnabledPlugins.FirstOrDefault(x => x.Id == id);
        }

    }
}