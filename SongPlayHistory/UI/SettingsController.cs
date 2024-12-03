using BeatSaberMarkupLanguage.Attributes;
using SongPlayHistory.Configuration;

namespace SongPlayHistory.UI
{
    public class SettingsController
    {
        [UIValue("enable-sph")]
        public bool EnableSongPlayHistory
        {
            get => PluginConfig.Instance.EnableSongPlayHistory;
            set => PluginConfig.Instance.EnableSongPlayHistory = value;
        }

        [UIValue("show-failed")]
        public bool ShowFailed
        {
            get => PluginConfig.Instance.ShowFailed;
            set => PluginConfig.Instance.ShowFailed = value;
        }

        [UIValue("average-accuracy")]
        public bool AverageAccuracy
        {
            get => PluginConfig.Instance.AverageAccuracy;
            set => PluginConfig.Instance.AverageAccuracy = value;
        }

        [UIValue("sort-by-date")]
        public bool SortByDate
        {
            get => PluginConfig.Instance.SortByDate;
            set => PluginConfig.Instance.SortByDate = value;
        }

        [UIValue("show-votes")]
        public bool ShowVotes
        {
            get => PluginConfig.Instance.ShowVotes;
            set => PluginConfig.Instance.ShowVotes = value;
        }
        
        [UIValue("enable-score-percentage")]
        public bool EnableScorePercentage
        {
            get => PluginConfig.Instance.EnableScorePercentage;
            set => PluginConfig.Instance.EnableScorePercentage = value;
        }
        
        [UIValue("highscore-percentage")]
        public bool ShowPercentageAtMenuHighScore
        {
            get => PluginConfig.Instance.ShowPercentageAtMenuHighScore;
            set => PluginConfig.Instance.ShowPercentageAtMenuHighScore = value;
        }
        
        [UIValue("result-percentage")]
        public bool ShowPercentageAtLevelEnd
        {
            get => PluginConfig.Instance.ShowPercentageAtLevelEnd;
            set => PluginConfig.Instance.ShowPercentageAtLevelEnd = value;
        }
        
        [UIValue("result-score-diff")]
        public bool ShowScoreDifferenceAtLevelEnd
        {
            get => PluginConfig.Instance.ShowScoreDifferenceAtLevelEnd;
            set => PluginConfig.Instance.ShowScoreDifferenceAtLevelEnd = value;
        }
        
        [UIValue("result-percentage-diff")]
        public bool ShowPercentageDifferenceAtLevelEnd
        {
            get => PluginConfig.Instance.ShowPercentageDifferenceAtLevelEnd;
            set => PluginConfig.Instance.ShowPercentageDifferenceAtLevelEnd = value;
        }

        [UIValue("multi-result-percentage")]
        public bool ShowPercentageAtMultiplayerResults
        {
            get => PluginConfig.Instance.ShowPercentageAtMultiplayerResults;
            set => PluginConfig.Instance.ShowPercentageAtMultiplayerResults = value;
        }
        
        [UIValue("multiplayer-info-installed")]
        public bool MultiplayerInfoInstalled => Plugin.Instance.MultiplayerInfoInstalled;
    }
}
