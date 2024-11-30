using System.Threading;
using System.Threading.Tasks;
using HMUI;
using IPA.Utilities.Async;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using SongPlayHistory.SongPlayData;
using UnityEngine;
using Zenject;

namespace SongPlayHistory.Patches;

public class MultiplayerResultsTablePatch: IAffinity
{
    [Inject]
    private readonly SiraLog _logger = null!;
    
    [Inject]
    private readonly IScoringCacheManager _scoringCacheManager = null!;
    
    // private BeatmapKey _beatmapKey = default;
    
    private CancellationTokenSource _cts = new CancellationTokenSource();
    
    private Task<LevelScoringCache>? _scoringCacheTask; 

    [AffinityPatch(typeof(MultiplayerResultsViewController), nameof(MultiplayerResultsViewController.Init))]
    [AffinityPrefix]
    private void MultiResultsGetBeatmapKey(BeatmapKey beatmapKey)
    {
        // _beatmapKey = beatmapKey;
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _scoringCacheTask = beatmapKey.IsValid() ? _scoringCacheManager.GetScoringInfo(beatmapKey, cancellationToken: token) : null;
    }

    [AffinityPatch(typeof(ResultsTableView), nameof(ResultsTableView.CellForIdx))]
    [AffinityPostfix]
    private void MultiResultsTableGetCell(ResultsTableView __instance, TableCell __result, int idx)
    {
        var cell = __result as ResultsTableCell;
        if (cell == null)
        {
            _logger.Warn("Cel is null or not ResultsTableCell");
            return;
        }

        var multiResult = __instance._dataList[idx];
        var cacheTask = _scoringCacheTask;
        
        if (cacheTask == null || cacheTask.IsCanceled) return;
        
        if (cacheTask.IsFaulted)
        {
            _logger.Warn("Failed to get scoring cache, can't show score percentage");
            if (cacheTask.Exception != null) _logger.Debug(cacheTask.Exception);
            return;
        }
        
        if (cacheTask.IsCompleted)
        {
            var cache = cacheTask.Result;
            ShowDataOnResultsTableCell(__instance, cell, multiResult, cache);
        }
        else
        {
            cacheTask.ContinueWith(task => ShowDataOnResultsTableCell(__instance, cell, multiResult, task.Result), 
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, UnityMainThreadTaskScheduler.Default);
        }
    }
    
    private void ShowDataOnResultsTableCell(ResultsTableView tableView, ResultsTableCell cell, MultiplayerPlayerResultsData multiData, LevelScoringCache scoringData)
    {
        var multiResults = multiData.multiplayerLevelCompletionResults;
        if (!multiResults.hasAnyResults || 
            multiResults.playerLevelEndState != MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndState.SongFinished
            || multiResults.levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
        {
            return;
        }

        var results = multiData.multiplayerLevelCompletionResults.levelCompletionResults;
        var percentage = results.multipliedScore / (float)scoringData.MaxMultipliedScore * 100;
        cell._scoreText.text = $"{ScoreFormatter.Format(results.cumulativeScore)} ({percentage:F2}%)";
        var min = cell._scoreText.rectTransform.offsetMin;
        min.x = -42;
        cell._scoreText.rectTransform.offsetMin = min;
    }
}