using SongPlayHistory.UI;
using SongPlayHistory.VoteTracker;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SPHUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<InMenuVoteTrackingHelper>().AsSingle();
        }
    }
}