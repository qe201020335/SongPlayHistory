using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

[PublicAPI]
public interface IScoringCacheManager
{
    public Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new());
}