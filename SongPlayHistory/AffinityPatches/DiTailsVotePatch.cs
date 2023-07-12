using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using SiraUtil.Affinity;
using Zenject;

namespace SongPlayHistory.AffinityPatches
{
    [HarmonyPatch]
    internal class DiTailsVotePatch
    {

        private static readonly Lazy<Type?> _ditails =
            new(() => PluginManager.GetPluginFromId("DiTails")?.Assembly.GetType("DiTails.UI.DetailViewHost"));
        
        private static readonly Lazy<MethodBase?> _method = 
            new(() => _ditails.Value?.GetMethod("Vote", BindingFlags.Instance | BindingFlags.NonPublic));

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
            Plugin.Log.Debug($"DiTails voted {upvote} to {____activeBeatmap.level.levelID}");
            UserVoteTracker.Vote(____activeBeatmap.level, upvote ? VoteType.UpVote : VoteType.DownVote);
        }
    }
}