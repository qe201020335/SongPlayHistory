using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using SiraUtil.Zenject;
using SongPlayHistory.Configuration;
using SongPlayHistory.Installers;
using SongPlayHistory.UI;
using SongPlayHistory.VoteTracker;
using Config = IPA.Config.Config;

namespace SongPlayHistory
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private const string HarmonyId = "com.github.qe201020335.SongPlayHistory";
        internal const string BeatSaverVotingId = "BeatSaverVoting";
        
        public static Plugin Instance { get; private set; } = null!;
        internal static Logger Log { get; private set; } = null!;
        
        private readonly Harmony _harmony;

        internal bool BeatSaverVotingInstalled { get; private set; }

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            _harmony = new Harmony(HarmonyId);

            PluginConfig.Instance = config.Generated<PluginConfig>();
            BSMLSettings.instance.AddSettingsMenu("Song Play History", "SongPlayHistory.UI.Settings.bsml", SettingsController.instance);
            
            zenjector.UseLogger();
            zenjector.Install<ScoreTrackerInstaller>(Location.Player);
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<AppInstaller>(Location.App);
        }

        [OnStart]
        public void OnStart()
        {
            // PatchSongListVoteIcon(PluginConfig.Instance.ShowVotes);
            BeatSaverVotingInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == BeatSaverVotingId);
            if (!Harmony.HasAnyPatches(HarmonyId))
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }


        // public void PatchSongListVoteIcon(bool enabled)
        // {
        //     if (BeatSaverVotingInstalled) return;  // let BeatSaverVoting do the job
        //     try
        //     {
        //         if (enabled && !Harmony.HasAnyPatches(HarmonyId))
        //         {
        //             Log.Info("Applying Harmony patches...");
        //             _harmony.PatchAll(Assembly.GetExecutingAssembly());
        //         }
        //         else if (!enabled && Harmony.HasAnyPatches(HarmonyId))
        //         {
        //             Log.Info("Removing Harmony patches...");
        //             _harmony.UnpatchSelf();
        //
        //             SetDataFromLevelAsync.OnUnpatch();
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Error("Error while applying Harmony patches.\n" + ex.ToString());
        //     }
        // }
    }
}
