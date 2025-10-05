using JetBrains.Annotations;
using SongPlayHistory.Model;

namespace SongPlayHistory.VoteTracker
{
    [PublicAPI]
    public interface IVoteTracker
    {
        public bool TryGetVote(BeatmapLevel level, out VoteType voteType);

        public void Vote(BeatmapLevel level, VoteType voteType);

    }
}