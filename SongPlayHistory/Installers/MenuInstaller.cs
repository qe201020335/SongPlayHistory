using SiraUtil.Logging;
using SongPlayHistory.Patches;
using SongPlayHistory.UI;
using SongPlayHistory.VoteTracker;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        
        [Inject]
        private readonly SiraLog _logger = null!;
        
        public override void InstallBindings()
        {
            _logger.Debug("Binding settings menu and manager");
            Container.BindInterfacesTo<MenuSettingsManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SettingsController>().AsSingle();
            
            _logger.Debug("Binding SPHUI");
            Container.BindInterfacesTo<SPHUI>().AsSingle();
            _logger.Debug("Binding InMenuVoteTrackingHelper");
            Container.BindInterfacesAndSelfTo<InMenuVoteTrackingHelper>().AsSingle().NonLazy();

            Container.BindInterfacesTo<LevelStatsViewPatch>().AsSingle();
        }
    }
}