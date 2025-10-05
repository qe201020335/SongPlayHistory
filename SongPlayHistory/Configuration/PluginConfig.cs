using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace SongPlayHistory.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; } = null!;
        public bool ShowFailed { get; set; } = true;
        public bool AverageAccuracy { get; set; } = true;
        public bool SortByDate { get; set; } = false;
        public bool ShowVotes { get; set; } = true;
    }
}
