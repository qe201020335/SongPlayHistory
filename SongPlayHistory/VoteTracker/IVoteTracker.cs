using SongPlayHistory.Model;

namespace SongPlayHistory.VoteTracker
{
    public interface IVoteTracker
    {
        public bool TryGetVote(IPreviewBeatmapLevel level, out VoteType voteType);

        public void Vote(IPreviewBeatmapLevel level, VoteType voteType);

    }
}