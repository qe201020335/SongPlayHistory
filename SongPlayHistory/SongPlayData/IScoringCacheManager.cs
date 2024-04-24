using System.Threading;
using System.Threading.Tasks;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

public interface IScoringCacheManager
{
    public Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new());
}