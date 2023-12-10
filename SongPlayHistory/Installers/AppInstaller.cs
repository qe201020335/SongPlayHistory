using System.Linq;
using IPA.Loader;
using SiraUtil.Logging;
using SongPlayHistory.SongPlayData;
using SongPlayHistory.VoteTracker;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class AppInstaller: Installer<AppInstaller>
    {

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<RecordsManager>().AsSingle();
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