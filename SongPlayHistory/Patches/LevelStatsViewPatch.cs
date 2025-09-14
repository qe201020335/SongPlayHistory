using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using SongPlayHistory.SongPlayData;
using TMPro;

namespace SongPlayHistory.Patches;

internal class LevelStatsViewPatch : IAffinity
{
    private readonly SiraLog _logger;
    private readonly IScoringCacheManager _scoringCacheManager;
    private readonly IRecordManager _recordManager;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    public LevelStatsViewPatch(SiraLog logger, IScoringCacheManager scoringCacheManager, IRecordManager recordManager)
    {
        _logger = logger;
        _scoringCacheManager = scoringCacheManager;
        _recordManager = recordManager;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(LevelStatsView), nameof(LevelStatsView.ShowStats), AffinityMethodType.Normal, null, typeof(PlayerLevelStatsData))]
    private void SetLevelStats(LevelStatsView __instance, PlayerLevelStatsData playerLevelStats)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        var highScore = playerLevelStats.validScore ? playerLevelStats.highScore : 0;
        var beatmapKey = playerLevelStats.GetBeatmapKey();
        
        var records = _recordManager.GetRecords(beatmapKey);
        if (records.Count > 0)
        {
            var sphHighScore = records.Max(record => record.ModifiedScore);
            highScore = Math.Max(playerLevelStats.highScore, sphHighScore);
            __instance._highScoreText.text = highScore.ToString();
        }

        if (highScore > 0) ShowScorePercentage(__instance._highScoreText, beatmapKey, highScore, _cts.Token);
    }

    private void ShowScorePercentage(TMP_Text text, BeatmapKey key, int highScore, CancellationToken token)
    {
        _logger.Trace($"Showing high score percentage for {key.levelId}");
        Task.Run(() => _scoringCacheManager.GetScoringInfo(key, cancellationToken: token), token)
            .ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    _logger.Warn("Failed to show percentage for high score.");
                    _logger.Warn(task.Exception);
                    return;
                }

                var cache = task.Result;
                var maxScore = cache.MaxMultipliedScore;
                if (maxScore <= 0) return;
                var percentage = (float)highScore / maxScore * 100;
                if (token.IsCancellationRequested) return;
                text.text = $"{highScore} ({percentage:0.00}%)";
            }, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, UnityMainThreadTaskScheduler.Default);
    }
}