using System.Threading;
using System.Threading.Tasks;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

public interface IScoringCacheManager
{
    public Task<LevelScoringCache> GetScoringInfo(IDifficultyBeatmap beatmap, CancellationToken cancellationToken = new());
}