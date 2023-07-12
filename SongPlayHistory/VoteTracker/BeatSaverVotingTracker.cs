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

        private readonly MethodBase? SongCoreGetCustomSongHash = 
            AccessTools.Method("SongCore.Utilities.Hashing:GetCustomLevelHash", new[] { typeof(CustomPreviewBeatmapLevel) });

        private Dictionary<string, BeatSaverVoting.Plugin.SongVote>? _votes
        {
            get
            {
                {
                    var votes = AccessTools.Field(typeof(BeatSaverVoting.Plugin), "votedSongs")?.GetValue(null) as Dictionary<string, BeatSaverVoting.Plugin.SongVote>;
                    if (votes == null)
                    {
                        _logger.Warn($"Can't get votedSongs field from BeatSaverVoting");
                    }
                    return votes;
                }
            }
        }

        private string GetHash(CustomPreviewBeatmapLevel customLevel)
        {
            string hash;
            if (SongCoreGetCustomSongHash != null)
            {
                hash = (string) SongCoreGetCustomSongHash.Invoke(null, new object[] { customLevel });
            }
            else
            {
                // BeatSaverVoting depends on SongCore so the reflection shouldn't fail.
                
                // This has some problem when there are duplicated levels
                // the level id would be custom_level_HASHHASHHASH_SONGFOLDERNAME
                hash = customLevel.levelID.Replace("custom_level_", "");
            }

            return hash.ToLower();
        }



        public void Vote(IPreviewBeatmapLevel level, VoteType voteType)
        {
            if (!(level is CustomPreviewBeatmapLevel customLevel)) return;
            try
            {
                var hash = GetHash(customLevel);
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
                var hash = GetHash(customLevel);
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