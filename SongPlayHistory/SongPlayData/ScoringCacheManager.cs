using System;
using System.Collections.Concurrent;
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
    private readonly BeatmapLevelsModel _beatmapLevelsModel = null!;
    
    [Inject]
    private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel = null!;
    
    [Inject]
    private readonly BeatmapDataLoader _beatmapDataLoader = null!;
    
    [Inject]
    private readonly SiraLog _logger = null!;
    
    //TODO use persistent storage cache if needed
    private readonly ConcurrentDictionary<BeatmapKey, LevelScoringCache> _cache = new ConcurrentDictionary<BeatmapKey, LevelScoringCache>();
    
    private readonly Dictionary<BeatmapKey, Task<LevelScoringCache>> _tasks = new Dictionary<BeatmapKey, Task<LevelScoringCache>>();
    
    public Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new())
    {
        if (_cache.TryGetValue(beatmapKey, out var cache))
        {
            _logger.Trace($"Scoring data cache hit for {beatmapKey.SerializedName()}: {cache}");
            return Task.FromResult(cache);
        }

        _logger.Trace($"Scoring data cache miss for {beatmapKey.SerializedName()}");
        Task<LevelScoringCache> loadTask;
        lock (_tasks)  // ConcurrentDictionary is not atomic for AddOrUpdate and the factory methods can be called multiple times
        {
            if (_tasks.TryGetValue(beatmapKey, out var existing))
            {
                if (existing.Status is TaskStatus.Canceled or TaskStatus.Faulted)
                {
                    _logger.Warn("Previous scoring data load task was cancelled or faulted, retrying.");
                    var newTask = LoadScoringInfo(beatmapKey, beatmapLevel, cancellationToken);
                    _tasks[beatmapKey] = newTask;
                    loadTask = newTask;
                }
                else
                {
                    loadTask = existing;
                }
            }
            else
            {
                _logger.Trace("No previous scoring data load task found, creating new task.");
                var newTask = LoadScoringInfo(beatmapKey, beatmapLevel, cancellationToken);
                _tasks[beatmapKey] = newTask;
                loadTask = newTask;
            }
        }
        
        return loadTask;
    }
    
    private async Task<LevelScoringCache> LoadScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel, CancellationToken cancellationToken)
    {
        _logger.Debug($"Loading scoring data for {beatmapKey.SerializedName()} on Thread {Environment.CurrentManagedThreadId}");

#if DEBUG
        var startTime = DateTime.Now;
#endif
        
        cancellationToken.ThrowIfCancellationRequested();
        // await Task.Delay(15000, cancellationToken); // simulate an extreme loading time

        if (beatmapLevel == null)
        {
            _logger.Trace("BeatmapLevel is null, getting from BeatmapLevelsModel.");
            beatmapLevel = _beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId);
            if (beatmapLevel == null)
            {
                _logger.Error("Failed to get BeatmapLevel.");
                throw new Exception("Failed to get BeatmapLevel.");
            }
        }
        
        _logger.Trace("Loading beat map level data from BeatmapLevelsModel.");
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

#if DEBUG
        var timeSpan = DateTime.Now - startTime;
        _logger.Info($"Took {timeSpan.TotalSeconds:F3} sec to load scoring data for {beatmapLevel.songName} ({beatmapKey.beatmapCharacteristic.compoundIdPartName}{beatmapKey.difficulty.SerializedName()})");
#endif
        
        cancellationToken.ThrowIfCancellationRequested();

        return newCache;
    }
}