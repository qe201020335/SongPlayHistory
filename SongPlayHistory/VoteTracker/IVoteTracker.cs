using SongPlayHistory.Model;

namespace SongPlayHistory.VoteTracker
{
    public interface IVoteTracker
    {
        public bool TryGetVote(BeatmapLevel level, out VoteType voteType);

        public void Vote(BeatmapLevel level, VoteType voteType);

    }
}