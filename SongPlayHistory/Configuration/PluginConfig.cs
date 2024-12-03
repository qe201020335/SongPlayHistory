using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace SongPlayHistory.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; } = null!;

        public bool EnableSongPlayHistory { get; set; } = true;
        public bool ShowFailed { get; set; } = true;
        public bool AverageAccuracy { get; set; } = true;
        public bool SortByDate { get; set; } = false;
        public bool ShowVotes { get; set; } = true;
        
        public bool EnableScorePercentage { get; set; } = true;
        public bool ShowPercentageAtMenuHighScore { get; set; } = true;
        public bool ShowPercentageAtLevelEnd { get; set; } = true;
        public bool ShowScoreDifferenceAtLevelEnd { get; set; } = true;
        public bool ShowPercentageDifferenceAtLevelEnd { get; set; } = true;
        public bool ShowPercentageAtMultiplayerResults { get; set; } = true;
    }
}
