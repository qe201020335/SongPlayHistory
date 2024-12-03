using SiraUtil.Logging;
using SongPlayHistory.Configuration;
using SongPlayHistory.Patches;
using SongPlayHistory.UI;
using SongPlayHistory.VoteTracker;
using Zenject;

namespace SongPlayHistory.Installers
{
    internal class MenuInstaller: Installer<MenuInstaller>
    {
        [Inject]
        private readonly SiraLog _logger = null!;
        
        [Inject]
        private readonly PluginConfig _config = null!;
        
        public override void InstallBindings()
        {
            _logger.Debug("Binding settings menu and manager");
            Container.BindInterfacesTo<MenuSettingsManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SettingsController>().AsSingle();

            if (_config.EnableSongPlayHistory)
            {
                _logger.Debug("Binding SPHUI");
                Container.BindInterfacesTo<SPHUI>().AsSingle();
            }

            _logger.Debug("Binding InMenuVoteTrackingHelper");
            Container.BindInterfacesAndSelfTo<InMenuVoteTrackingHelper>().AsSingle().NonLazy();

            // Score Percentage features
            if (_config.EnableScorePercentage)
            {
                if (_config.ShowPercentageAtMenuHighScore)
                {
                    Container.BindInterfacesTo<LevelStatsViewPatch>().AsSingle();
                }

                if (_config.ShowPercentageAtLevelEnd || _config.ShowScoreDifferenceAtLevelEnd)
                {
                    Container.BindInterfacesTo<ResultsViewControllerPatch>().AsSingle();
                }

                if (_config.ShowPercentageAtMultiplayerResults)
                {
                    Container.BindInterfacesTo<MultiplayerResultsTablePatch>().AsSingle();
                }
            }
        }
    }
}