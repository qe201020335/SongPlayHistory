namespace SongPlayHistory.SongPlayTracking;

public class LevelCompletionResultsExtraData
{
    public GameplayCoreSceneSetupData SceneSetupData { get; }

    public ScoreRecord ScoringData { get; }

    public ScoreRecord? ScoringDataWhenEnergyReached0 { get; }

    public bool ScoreSubmissionDisabled { get; }

    public bool IsMultiplayer { get; }

    public bool IsParty { get; }
    
    public PlayerLevelStatsData? PreviousPlayerLevelStats { get; }

    public bool IsPractice => SceneSetupData.practiceSettings != null;

    public bool EnergyDidReach0 => ScoringDataWhenEnergyReached0 != null;


    internal LevelCompletionResultsExtraData(GameplayCoreSceneSetupData setupData, ScoreRecord scoringData, ScoreRecord? scoringDataWhenEnergyReached0,
        bool scoreSubmissionDisabled, bool isMulti, bool isParty, PlayerLevelStatsData? previousPlayerLevelStats)
    {
        SceneSetupData = setupData;
        ScoringData = scoringData;
        ScoringDataWhenEnergyReached0 = scoringDataWhenEnergyReached0;
        ScoreSubmissionDisabled = scoreSubmissionDisabled;
        IsMultiplayer = isMulti;
        IsParty = isParty;
        PreviousPlayerLevelStats = previousPlayerLevelStats;
    }
}