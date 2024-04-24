using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using Zenject;
using BSVType = BeatSaverVoting.Plugin.VoteType;

namespace SongPlayHistory.VoteTracker
{
    internal class BeatSaverVotingTracker: IVoteTracker
    {

        [Inject]
        private readonly SiraLog _logger = null!;

        private Dictionary<string, BeatSaverVoting.Plugin.SongVote>? _votes
        {
            get
            {
                {
                    var votes = AccessTools.Field(typeof(BeatSaverVoting.Plugin), "votedSongs")?.GetValue(null) as Dictionary<string, BeatSaverVoting.Plugin.SongVote>;
                    if (votes == null)
                    {
                        _logger.Warn("Can't get votedSongs field from BeatSaverVoting");
                    }
                    return votes;
                }
            }
        }

        public void Vote(IPreviewBeatmapLevel level, VoteType voteType)
        {
            if (!(level is CustomPreviewBeatmapLevel customLevel)) return;
            try
            {
                var hash = Utils.Utils.GetCustomLevelHash(customLevel);
                var bsvType = voteType == VoteType.Upvote ? BSVType.Upvote : BSVType.Downvote;
                if (_votes == null)
                {
                    return;
                }
                
                if (_votes.ContainsKey(hash) && _votes[hash].voteType == bsvType) return;
                
                _votes[hash] = new BeatSaverVoting.Plugin.SongVote(hash, bsvType);
                BeatSaverVoting.Plugin.WriteVotes();
            }
            catch (Exception e)
            {
                _logger.Critical($"Failed to track vote for {level.songName}, {level.levelID}");
                _logger.Critical(e);
            }
        }

        public bool TryGetVote(IPreviewBeatmapLevel level, out VoteType voteType)
        {
            voteType = VoteType.Downvote;
            if (!(level is CustomPreviewBeatmapLevel customLevel)) return false;
            try
            {
                var hash = Utils.Utils.GetCustomLevelHash(customLevel);
                var voteStatus = BeatSaverVoting.Plugin.CurrentVoteStatus(hash);
                if (voteStatus == null)
                {
                    return false;
                }

                if (voteStatus == BSVType.Upvote)
                {
                    voteType = VoteType.Upvote;
                    return true;
                }
                
                voteType = VoteType.Downvote;
                return true;
            }
            catch (Exception e)
            {
                _logger.Warn($"Can't get vote from BeatSaverVoting: {e.Message}");
                _logger.Debug(e);
                return false;
            }
        }
    }
}