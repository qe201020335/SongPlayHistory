using System;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using Zenject;
using BSVType = BeatSaverVoting.Plugin.VoteType;
using BSVPlugin = BeatSaverVoting.Plugin;

namespace SongPlayHistory.VoteTracker
{
    internal class BeatSaverVotingTracker: IVoteTracker
    {

        [Inject]
        private readonly SiraLog _logger = null!;
        
        public void Vote(IPreviewBeatmapLevel level, VoteType voteType)
        {
            if (!(level is CustomPreviewBeatmapLevel customLevel)) return;
            try
            {
                _logger.Debug($"Voting for {level.songName}, {level.levelID}, {voteType}");
                if (BSVPlugin.votedSongs == null)
                {
                    _logger.Debug("BeatSaverVoting dictionary is null");
                    return;
                }

                var hash = Utils.Utils.GetLowerCaseCustomLevelHash(customLevel);
                var bsvType = voteType == VoteType.Upvote ? BSVType.Upvote : BSVType.Downvote;
                if (BSVPlugin.votedSongs.TryGetValue(hash, out var songVote) && songVote.voteType == bsvType) return;
                
                BSVPlugin.votedSongs[hash] = new BSVPlugin.SongVote(hash, bsvType);
                BSVPlugin.WriteVotes();
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
                _logger.Debug($"Getting vote data for {level.songName}, {level.levelID}");
                var hash = Utils.Utils.GetLowerCaseCustomLevelHash(customLevel);
                
                var voteStatus = BSVPlugin.CurrentVoteStatus(hash);
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