using System;
using System.Collections.Generic;
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
    private readonly PlayerDataModel _playerDataModel = null!;
    
    [Inject]
    private readonly BeatmapLevelsModel _beatmapLevelsModel = null!;
    
    [Inject]
    private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel;
    
    [Inject]
    private readonly BeatmapDataLoader _beatmapDataLoader = null!;
    
    [Inject]
    private readonly EnvironmentsListModel _environmentListModel = null!;
    
    [Inject]
    private readonly SiraLog _logger = null!;
    
    //TODO use persistent storage cache if needed
    private readonly Dictionary<LevelMapKey, LevelScoringCache> _cache = new Dictionary<LevelMapKey, LevelScoringCache>();
    
    public async Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new())
    {
        _logger.Debug($"Get scoring cache from Thread {Environment.CurrentManagedThreadId}");
        var cacheKey = new LevelMapKey(beatmapKey);
        
        lock (_cache)
        {
            if (_cache.TryGetValue(cacheKey, out var cache))
            {
                return cache;
            }
        }
        
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
        
        var basicBeatmapData = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
        var envName = basicBeatmapData.environmentName;
        var envInfo = _environmentListModel.GetEnvironmentInfoBySerializedNameSafe(envName);
        var beatmapData = await _beatmapDataLoader.LoadBeatmapDataAsync(
                beatmapLevelData, 
                beatmapKey, 
                beatmapLevel.beatsPerMinute, 
                false,
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

        lock (_cache)
        {
            // write cache
            _cache[cacheKey] = newCache;
        }
        cancellationToken.ThrowIfCancellationRequested();

        return newCache;
    }
}