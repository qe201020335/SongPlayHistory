using IPA.Loader;
using SongPlayHistory.SongPlayData;
using SongPlayHistory.SongPlayTracking;
using SongPlayHistory.VoteTracker;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class AppInstaller: Installer<AppInstaller>
    {
        public override void InstallBindings()
        {
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