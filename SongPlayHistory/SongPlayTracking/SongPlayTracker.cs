using System;
using BS_Utils.Gameplay;
using SiraUtil.Logging;
using SiraUtil.Submissions;
using Zenject;

namespace SongPlayHistory.SongPlayTracking;

internal class SongPlayTracker : IInitializable, IDisposable
{
    public static event Action<LevelCompletionResults, LevelCompletionResultsExtraData>? StandardMultiLevelDidFinish;

    [Inject]
    private readonly SiraLog _logger = null!;

    [Inject]
    private readonly ScoreTracker _scoreTracker = null!;

    [Inject]
    private readonly ExtraCompletionDataManager _extraCompletionDataManager = null!;

    [Inject]
    private readonly Submission _siraSubmission = null!;

    [Inject]
    private readonly GameplayCoreSceneSetupData _sceneSetupData = null!;

    [InjectOptional]
    private readonly StandardLevelScenesTransitionSetupDataSO? _standardTransitionData = null;

    [InjectOptional]
    private readonly IMultiplayerLevelEndActionsPublisher? _multiplayerLevelEndActions = null;

    void IInitializable.Initialize()
    {
        if (_standardTransitionData != null)
        {
            _standardTransitionData.didFinishEvent += OnStandardLevelDidFinish;
        }
        else if (_multiplayerLevelEndActions != null)
        {
            _multiplayerLevelEndActions.playerDidFinishEvent += OnMultiplayerLevelDidFinish;
        }
        else
        {
            _logger.Warn("Not standard or multiplayer active player! This should not happen!");
        }
    }

    void IDisposable.Dispose()
    {
        if (_standardTransitionData != null)
        {
            _standardTransitionData.didFinishEvent -= OnStandardLevelDidFinish;
        }

        if (_multiplayerLevelEndActions != null)
        {
            _multiplayerLevelEndActions.playerDidFinishEvent -= OnMultiplayerLevelDidFinish;
        }
    }

    private void OnStandardLevelDidFinish(StandardLevelScenesTransitionSetupDataSO? data, LevelCompletionResults? results)
    {
        if (data == null) return;
        HandleLevelFinished(results, false, data.gameMode.Equals("Party", StringComparison.OrdinalIgnoreCase));
    }

    private void OnMultiplayerLevelDidFinish(MultiplayerLevelCompletionResults? results)
    {
        if (results == null) return;
        HandleLevelFinished(results.levelCompletionResults, true, false);
    }

    private void HandleLevelFinished(LevelCompletionResults? results, bool isMulti, bool isParty)
    {
        if (results == null)
        {
            _logger.Warn("LevelCompletionResults is null.");
            return;
        }

        if (Utils.Utils.IsInReplay())
        {
            _logger.Info("It was a replay, ignored.");
            return;
        }

        _logger.Debug("Standard/Multi level finished, preparing extra results data");

        var ssd = _siraSubmission.Tickets().Length > 0 || ScoreSubmission.Disabled || ScoreSubmission.ProlongedDisabled;

        var extraData = new LevelCompletionResultsExtraData(_sceneSetupData, _scoreTracker.GetImmediateScoreData(), _scoreTracker.FailScoreRecord,
            ssd, isMulti, isParty);

        _extraCompletionDataManager.AddExtraData(results, extraData);

        var e = StandardMultiLevelDidFinish;

        try
        {
            e?.Invoke(results, extraData);
        }
        catch (Exception exception)
        {
            _logger.Error("Exception caught while emitting level did finish event.");
            _logger.Error(exception);
        }
    }
}