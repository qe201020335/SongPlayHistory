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
            Container.BindInterfacesTo<SPHUI>().AsSingle();
            _logger.Debug("Binding InMenuVoteTrackingHelper");
            Container.BindInterfacesAndSelfTo<InMenuVoteTrackingHelper>().AsSingle().NonLazy();
        }
    }
}