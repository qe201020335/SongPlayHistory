using BeatSaberMarkupLanguage.Attributes;
using SongPlayHistory.Configuration;

namespace SongPlayHistory.UI
{
    public class SettingsController
    {
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
    }
}
