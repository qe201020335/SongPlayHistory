using Zenject;

namespace SongPlayHistory.Installers
{
    public class AppInstaller: Installer<AppInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<UserVoteTracker>().AsSingle();
            Container.BindInterfacesAndSelfTo<RecordsManager>().AsSingle();
        }
    }
}