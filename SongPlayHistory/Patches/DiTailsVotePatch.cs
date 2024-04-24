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
        private static readonly MethodBase? DiTailsVote = AccessTools.Method("DiTails.UI.DetailViewHost:Vote", new[] { typeof(bool) });

        [HarmonyTargetMethod]
        private static MethodBase CalculateMethod()
        {
            return DiTailsVote!;
        }
        
        [HarmonyPrepare]
        private static bool Prepare()
        {
            return DiTailsVote != null;
        }

        [HarmonyPrefix]
        public static void Prefix(bool upvote, BeatmapLevel? ____activeBeatmap)  //TODO: update when DiTails Update
        {
            if (____activeBeatmap == null) return;
            var vote = upvote ? VoteType.Upvote : VoteType.Downvote;
            Plugin.Log.Debug($"DiTails voted {vote} to {____activeBeatmap.levelID}");
            InMenuVoteTrackingHelper.Instance?.Vote(____activeBeatmap, vote);
        }
    }
}