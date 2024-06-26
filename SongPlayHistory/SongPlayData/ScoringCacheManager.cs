﻿using System;
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
    private readonly SiraLog _logger = null!;
    
    //TODO use persistent storage cache if needed
    private readonly Dictionary<LevelMapKey, LevelScoringCache> _cache = new Dictionary<LevelMapKey, LevelScoringCache>();
    
    public async Task<LevelScoringCache> GetScoringInfo(IDifficultyBeatmap beatmap, CancellationToken cancellationToken = new())
    {
        _logger.Debug($"Get scoring cache from Thread {Environment.CurrentManagedThreadId}");
        var cacheKey = new LevelMapKey(beatmap);
        
        lock (_cache)
        {
            if (_cache.TryGetValue(cacheKey, out var cache))
            {
                return cache;
            }
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        // await Task.Delay(15000, cancellationToken); // simulate am extreme loading time
        
        var beatmapData = await beatmap.GetBeatmapDataAsync(beatmap.GetEnvironmentInfo(), _playerDataModel.playerData.playerSpecificSettings);
        cancellationToken.ThrowIfCancellationRequested();
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