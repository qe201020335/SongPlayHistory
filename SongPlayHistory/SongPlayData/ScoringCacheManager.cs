using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using Zenject;

namespace SongPlayHistory.SongPlayData;

internal class ScoringCacheManager: IScoringCacheManager
{
    [Inject]
    private readonly BeatmapLevelsModel _beatmapLevelsModel = null!;
    
    [Inject]
    private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel = null!;
    
    [Inject]
    private readonly BeatmapDataLoader _beatmapDataLoader = null!;
    
    [Inject]
    private readonly SiraLog _logger = null!;
    
    //TODO use persistent storage cache if needed
    private readonly ConcurrentDictionary<BeatmapKey, LevelScoringCache> _cache = new ConcurrentDictionary<BeatmapKey, LevelScoringCache>();
    
    private readonly ConcurrentDictionary<BeatmapKey, Task<LevelScoringCache>> _tasks = new ConcurrentDictionary<BeatmapKey, Task<LevelScoringCache>>();
    
    public Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new())
    {
        if (_cache.TryGetValue(beatmapKey, out var cache))
        {
            _logger.Trace($"Using scoring data from cache for {beatmapKey.SerializedName()}: {cache}");
            return Task.FromResult(cache);
        }
        
        var loadTask = _tasks.GetOrAdd(beatmapKey, key => LoadScoringInfo(key, beatmapLevel, cancellationToken));
        if (loadTask.Status is TaskStatus.Canceled or TaskStatus.Faulted)
        {
            loadTask = _tasks[beatmapKey] = LoadScoringInfo(beatmapKey, beatmapLevel, cancellationToken);
        }
        
        return loadTask;
    }
    
    private async Task<LevelScoringCache> LoadScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel, CancellationToken cancellationToken)
    {
        _logger.Debug($"Loading scoring data for {beatmapKey.SerializedName()}");
        
        cancellationToken.ThrowIfCancellationRequested();
        // await Task.Delay(15000, cancellationToken); // simulate an extreme loading time

        if (beatmapLevel == null)
        {
            _logger.Warn("BeatmapLevel is null, getting from BeatmapLevelsModel.");
            beatmapLevel = _beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId);
            if (beatmapLevel == null)
            {
                _logger.Error("Failed to get BeatmapLevel.");
                throw new Exception("Failed to get BeatmapLevel.");
            }
        }
        
        _logger.Debug("Loading beat map level data from BeatmapLevelsModel.");
        var dataVersion = await _beatmapLevelsEntitlementModel.GetLevelDataVersionAsync(beatmapKey.levelId, cancellationToken);
        var loadResult = await _beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapKey.levelId, dataVersion, cancellationToken);
        if (loadResult.isError)
        {
            _logger.Error("Failed to get BeatmapLevelData.");
            throw new Exception("Failed to load beat map level data.");
        }
        
        var beatmapLevelData = loadResult.beatmapLevelData!;
        
        var beatmapData = await _beatmapDataLoader.LoadBeatmapDataAsync(
                beatmapLevelData, 
                beatmapKey, 
                beatmapLevel.beatsPerMinute, 
                false,
                null,
                null,
                dataVersion,
                null,
                null,
                false);

        cancellationToken.ThrowIfCancellationRequested();
        
        if (beatmapData == null)
        {
            _logger.Error("Failed to get BeatmapData.");
            throw new Exception("Failed to get BeatmapData.");
        }

        var notesCount = beatmapData.cuttableNotesCount;
        var fullMaxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(beatmapData);
        // we can use the original v2 scoring method to calculate the adjusted max score if there is no slider or burst
        var isV2Score = !beatmapData.GetBeatmapDataItems<SliderData>(0).Any();
        cancellationToken.ThrowIfCancellationRequested();

        var newCache = new LevelScoringCache
        {
            MaxMultipliedScore = fullMaxScore,
            NotesCount = notesCount,
            IsV2Score = isV2Score
        };

        // write cache
        _cache[beatmapKey] = newCache;

        cancellationToken.ThrowIfCancellationRequested();

        return newCache;
    }
}