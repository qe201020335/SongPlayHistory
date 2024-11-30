using System.Threading;
using System.Threading.Tasks;
using HMUI;
using IPA.Utilities.Async;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using SongPlayHistory.SongPlayData;
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
            RestoreSize(cell, tableView._cellPrefab);
            return;
        }
        
        MakeRoom(cell, tableView._cellPrefab);
        var results = multiData.multiplayerLevelCompletionResults.levelCompletionResults;
        var percentage = results.multipliedScore / (float)scoringData.MaxMultipliedScore;
        cell._rankText.text = $"{percentage:P2}";
    }

    private void MakeRoom(ResultsTableCell cell, ResultsTableCell prefab)
    {
        var scoreMin = prefab._scoreText.rectTransform.offsetMin;
        var scoreMax = prefab._scoreText.rectTransform.offsetMax;
        var rankMin = prefab._rankText.rectTransform.offsetMin;
        rankMin.x -= 6;
        scoreMax.x -= 6;
        scoreMin.x -= 6;
        cell._rankText.rectTransform.offsetMin = rankMin;
        cell._scoreText.rectTransform.offsetMin = scoreMin;
        cell._scoreText.rectTransform.offsetMax = scoreMax;
    }

    private void RestoreSize(ResultsTableCell cell, ResultsTableCell prefab)
    {
        var scoreMin = prefab._scoreText.rectTransform.offsetMin;
        var scoreMax = prefab._scoreText.rectTransform.offsetMax;
        var rankMin = prefab._rankText.rectTransform.offsetMin;
        cell._rankText.rectTransform.offsetMin = rankMin;
        cell._scoreText.rectTransform.offsetMin = scoreMin;
        cell._scoreText.rectTransform.offsetMax = scoreMax;
    }
}