using System;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using SongPlayHistory.Model;
using SongPlayHistory.VoteTracker;

namespace SongPlayHistory.Patches
{
    [HarmonyPatch]
    internal class DiTailsVotePatch
    {

        private static readonly Lazy<Type?> _ditails =
            new(() => PluginManager.GetPluginFromId("DiTails")?.Assembly.GetType("DiTails.UI.DetailViewHost"));
        
        private static readonly Lazy<MethodBase?> _method = 
            new(() => _ditails.Value?.GetMethod("Vote", BindingFlags.Instance | BindingFlags.NonPublic));
        
        // TODO AccessTools.Property(AccessTools.TypeByName("BeatLeader.Replayer.ReplayerLauncher"), "IsStartedAsReplay")?.GetGetMethod(false)

        [HarmonyTargetMethod]
        private static MethodBase CalculateMethod()
        {
            return _method.Value!;
        }
        
        [HarmonyPrepare]
        private static bool Prepare()
        {
            Plugin.Log.Debug($"DiTailsVotePatch::Prepare: {_method.Value != null}");
            return _method.Value != null;
        }

        [HarmonyPrefix]
        public static void Prefix(bool upvote, IDifficultyBeatmap? ____activeBeatmap)
        {
            if (____activeBeatmap == null) return;
            var vote = upvote ? VoteType.Upvote : VoteType.Downvote;
            Plugin.Log.Debug($"DiTails voted {vote} to {____activeBeatmap.level.levelID}");
            InMenuVoteTrackingHelper.Instance?.Vote(____activeBeatmap.level, vote);
        }
    }
}