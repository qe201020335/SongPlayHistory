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

    private readonly Dictionary<BeatmapKey, LoadTask> _tasks = new Dictionary<BeatmapKey, LoadTask>();

    public Task<LevelScoringCache> GetScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel = null, CancellationToken cancellationToken = new())
    {
        if (_cache.TryGetValue(beatmapKey, out var cache))
        {
            _logger.Trace($"Scoring data cache hit for {beatmapKey.SerializedName()}: {cache}");
            return Task.FromResult(cache);
        }

        _logger.Trace($"Scoring data cache miss for {beatmapKey.SerializedName()}");
        Task<LevelScoringCache> resultTask;
        var cancellable = cancellationToken.CanBeCanceled;
        lock (_tasks)  // ConcurrentDictionary is not atomic for AddOrUpdate and the factory methods can be called multiple times
        {
            if (_tasks.TryGetValue(beatmapKey, out var loadTask))
            {
                var task = loadTask.Task;
                if (task.Status is TaskStatus.Canceled or TaskStatus.Faulted)
                {
                    _logger.Warn("Previous scoring data load task was cancelled or faulted, retrying.");
                    loadTask = CreateAndAddLoadTask(beatmapKey, beatmapLevel, cancellable);
                }
            }
            else
            {
                _logger.Trace("No previous scoring data load task found, creating new task.");
                loadTask = CreateAndAddLoadTask(beatmapKey, beatmapLevel, cancellable);
            }

            if (cancellable)
            {
                resultTask = CreateCancellableTaskFromLoadTask(loadTask, cancellationToken);
                loadTask.RequestOne();
            }
            else
            {
                resultTask = Task.Run(async () => await loadTask.Task, cancellationToken);
                loadTask.SetNotCancellable();
            }
        }
        
        return resultTask;
    }

    private Task<LevelScoringCache> CreateCancellableTaskFromLoadTask(LoadTask loadTask, CancellationToken cancellationToken)
    {
        var cancelTaskSource = new TaskCompletionSource<bool>();
        cancellationToken.Register(() =>
        {
            cancelTaskSource.TrySetResult(true);
            lock (_tasks)
            {
                var beatmapKey = loadTask.BeatmapKey;
                if (loadTask.CancelOne())
                {
                    _logger.Trace($"Scoring data load for {beatmapKey.SerializedName()} was cancelled.");
                    if (_tasks.TryGetValue(beatmapKey, out var taskInDictionary) && taskInDictionary == loadTask)
                    {
                        _tasks.Remove(beatmapKey);
                    }
                }
            }
        });
        var resultTask = Task.Run(async () =>
        {
            var loadingTask = loadTask.Task;
            var finishedTask = await Task.WhenAny(loadingTask, cancelTaskSource.Task);
            if (finishedTask == loadingTask)
            {
                return await loadTask.Task;
            }
                    
            throw new TaskCanceledException();
        }, cancellationToken);
        return resultTask;
    }

    private LoadTask CreateAndAddLoadTask(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel, bool isCancellable)
    {
        var cts = isCancellable ? new CancellationTokenSource() : null;
        var loadingTask = LoadScoringInfo(beatmapKey, beatmapLevel, cts?.Token ?? CancellationToken.None);
        var entry = new LoadTask(beatmapKey, loadingTask, cts);
        loadingTask.ContinueWith(task =>
        {
#if DEBUG
            _logger.Debug($"Load task for {beatmapKey.SerializedName()} finished with status {task.Status}");
#endif
            // it either succeeded with data written into the cache dictionary or exploded (cancel or fault)
            lock (_tasks)
            {
                entry.SetFinishedOrFaulted();
                // a new one may have been added because it gets the lock before us and we are faulted
                if (_tasks.TryGetValue(beatmapKey, out var request) && request.Task == task)
                {
                    _tasks.Remove(beatmapKey); 
                }
            }
        }, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
        _tasks[beatmapKey] = entry;
        return entry;
    }
    
    private async Task<LevelScoringCache> LoadScoringInfo(BeatmapKey beatmapKey, BeatmapLevel? beatmapLevel, CancellationToken cancellationToken)
    {
        _logger.Debug($"Loading scoring data for {beatmapKey.SerializedName()} on Thread {Environment.CurrentManagedThreadId}");

        if (_cache.TryGetValue(beatmapKey, out var cache))
        {
            // rare case where (in order):
            // - a request checked cache and it missed
            // - the previous load wrote cache
            // - the previous load got cancelled and threw before return
            // - the request got the lock before the task clean up logic
            // - the request checked that the existing was cancelled
            // - the request created a new task
            _logger.Trace($"Scoring data cache hit for {beatmapKey.SerializedName()}: {cache}");
            return cache;
        }

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
    
    private class LoadTask
    {
        public readonly BeatmapKey BeatmapKey;
        
        public readonly Task<LevelScoringCache> Task;
    
        private readonly CancellationTokenSource? _cancellationTokenSource;
        
        private int _waitCount = 0;
        
        private bool _cancellable;
        
        private bool _cancelled = false;
        
        private bool _finished = false;
        
        public LoadTask(BeatmapKey beatmapKey, Task<LevelScoringCache> task, CancellationTokenSource? cancellationTokenSource)
        {
            BeatmapKey = beatmapKey;
            Task = task;
            _cancellationTokenSource = cancellationTokenSource;
            _cancellable = cancellationTokenSource != null;
        }
        
        public void RequestOne()
        {
            if (!_cancellable) return;
            if (_cancelled) throw new InvalidOperationException("This loading task has already been cancelled.");
            _waitCount++;
        }

        public void SetFinishedOrFaulted()
        {
            if (_cancelled || _finished) return;
            _finished = true;
            _cancellationTokenSource?.Dispose();
        }

        public void SetNotCancellable()
        {
            _cancellable = false;
            _waitCount = 0;
        }

        /// <returns>True if the underlying task is cancelled because of this call,
        /// False if there are still others waiting, or it has already been cancelled </returns>
        public bool CancelOne()
        {
            if (_finished || _cancelled || !_cancellable) return false;
            _waitCount--;
            if (_waitCount <= 0)
            {
                _cancelled = true;
                _cancellationTokenSource!.Cancel();
                _cancellationTokenSource?.Dispose();
                return true;
            }

            return false;
        }
    }
}