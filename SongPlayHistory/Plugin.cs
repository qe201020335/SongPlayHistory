using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
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
        internal const string MultiplayerInfoId = "MultiplayerInfo";
        
        public static Plugin Instance { get; private set; } = null!;
        internal static Logger Log { get; private set; } = null!;
        
        private readonly Harmony _harmony;

        internal bool BeatSaverVotingInstalled { get; private set; }
        internal bool MultiplayerInfoInstalled { get; private set; }

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            _harmony = new Harmony(HarmonyId);

            var pluginConfig = config.Generated<PluginConfig>();
            PluginConfig.Instance = pluginConfig;
            
            zenjector.UseLogger();
            zenjector.Install<SongPlayTrackingInstaller>(Location.MultiPlayer | Location.StandardPlayer);
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        }

        [OnStart]
        public void OnStart()
        {
            BeatSaverVotingInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == BeatSaverVotingId);
            MultiplayerInfoInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == MultiplayerInfoId);
            if (!Harmony.HasAnyPatches(HarmonyId))
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        [OnExit]
        public void OnExit()
        {
            
        }
    }
}
