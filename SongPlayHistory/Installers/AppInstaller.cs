using IPA.Loader;
using SongPlayHistory.Configuration;
using SongPlayHistory.SongPlayData;
using SongPlayHistory.SongPlayTracking;
using SongPlayHistory.VoteTracker;
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
            
            var bsVoting = PluginManager.GetPluginFromId(Plugin.BeatSaverVotingId) != null;

            if (bsVoting)
            {
                Plugin.Log.Info("BeatSaverVoting is installed! Binding BeatSaverVotingTracker.");
                Container.BindInterfacesTo<BeatSaverVotingTracker>().AsSingle();
            }
            else
            {
                Plugin.Log.Info("BeatSaverVoting is NOT installed! Binding InternalVoteTracker.");
                Container.BindInterfacesTo<InternalVoteTracker>().AsSingle();
            }
        }
    }
}