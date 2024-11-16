using SongPlayHistory.SongPlayTracking;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class SongPlayTrackingInstaller : Installer<SongPlayTrackingInstaller>
    {
        public override void InstallBindings()
        {
            Plugin.Log.Warn("Binding ScoreTracker");
            Container.BindInterfacesAndSelfTo<ScoreTracker>().AsSingle();
            Container.BindInterfacesTo<SongPlayTracker>().AsSingle();
        }
    }
}