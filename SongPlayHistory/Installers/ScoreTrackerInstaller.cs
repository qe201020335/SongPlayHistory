using SongPlayHistory.Utils;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class ScoreTrackerInstaller : Installer<ScoreTrackerInstaller>
    {
        public override void InstallBindings()
        {
            Plugin.Log.Warn("Binding ScoreTracker");
            Container.BindInterfacesTo<ScoreTracker>().AsSingle();
        }
    }
}