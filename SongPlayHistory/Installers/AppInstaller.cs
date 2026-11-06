using SongPlayHistory.Configuration;
using SongPlayHistory.SongPlayData;
using SongPlayHistory.SongPlayTracking;
using Zenject;

namespace SongPlayHistory.Installers
{
    internal class AppInstaller: Installer<AppInstaller>
    {
        private readonly PluginConfig _config;
        
        public AppInstaller(PluginConfig config)
        {
            _config = config;
        }
        
        public override void InstallBindings()
        {
            Container.BindInstance(_config);

            Container.BindInterfacesTo<RecordsManager>().AsSingle();
            Container.BindInterfacesTo<ScoringCacheManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<ExtraCompletionDataManager>().AsSingle();
        }
    }
}