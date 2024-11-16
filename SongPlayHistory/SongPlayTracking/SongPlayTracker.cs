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

    // This type is actually binded in PCInit so it will never be null
    [InjectOptional]
    private readonly StandardLevelScenesTransitionSetupDataSO? _standardTransitionData = null;

    // Use this instead of MultiplayerLevelScenesTransitionSetupDataSO to trigger result gathering
    // as soon as the player fail and/or becomes inactive, instead of waiting for return to lobby.
    [InjectOptional]
    private readonly IMultiplayerLevelEndActionsPublisher? _multiplayerLevelEndActions = null;

    void IInitializable.Initialize()
    {
        _logger.Trace("Initializing SongPlayTracker");
        if (_standardTransitionData != null) // this will always be true
        {
            _standardTransitionData.didFinishEvent += OnStandardLevelDidFinish;
        }

        if (_multiplayerLevelEndActions != null)
        {
            _logger.Trace($"Type of multi end actions publisher from di is: {_multiplayerLevelEndActions.GetType()}");
            _multiplayerLevelEndActions.playerDidFinishEvent += OnMultiplayerLevelDidFinish;
        }
    }

    void IDisposable.Dispose()
    {
        _logger.Trace("Disposing SongPlayTracker");
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
        _logger.Trace("Standard level finished");
        if (data == null)
        {
            _logger.Warn("StandardLevelScenesTransitionSetupDataSO is null.");
            return;
        }

        HandleLevelFinished(results, false, data.gameMode.Equals("Party", StringComparison.OrdinalIgnoreCase));
    }

    private void OnMultiplayerLevelDidFinish(MultiplayerLevelCompletionResults? results)
    {
        _logger.Trace("Multi level finished");
        if (results == null)
        {
            _logger.Warn("MultiplayerLevelCompletionResults is null.");
            return;
        }

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