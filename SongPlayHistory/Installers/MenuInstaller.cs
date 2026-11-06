using SiraUtil.Logging;
using SongPlayHistory.UI;
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
        }
    }
}