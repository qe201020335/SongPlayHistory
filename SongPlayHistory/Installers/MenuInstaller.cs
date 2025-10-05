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
        
        public override void InstallBindings()
        {
            _logger.Debug("Binding settings menu and manager");
            Container.BindInterfacesTo<MenuSettingsManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SettingsController>().AsSingle();

            _logger.Debug("Binding SPHUI");
            Container.BindInterfacesTo<SPHUI>().AsSingle();

            _logger.Debug("Binding InMenuVoteTrackingHelper");
            Container.BindInterfacesAndSelfTo<InMenuVoteTrackingHelper>().AsSingle().NonLazy();
        }
    }
}