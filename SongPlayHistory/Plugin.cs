using System;
using System.Diagnostics;
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
        private const string BeatSaverVotingId = "BeatSaverVoting";
        private const string DiTailsId = "DiTails";
        private const string MultiplayerInfoId = "MultiplayerInfo";
        private const string ScoreSaberId = "ScoreSaber";
        private const string BeatLeaderId = "BeatLeader";
        
        public static Plugin Instance { get; private set; } = null!;
        internal static Logger Log { get; private set; } = null!;
        
        private readonly Harmony _harmony;

        internal bool BeatSaverVotingInstalled { get; }
        internal bool MultiplayerInfoInstalled { get; }

        internal PluginMetadata? SSMetadata { get; }
        internal PluginMetadata? BLMetadata { get; }
        internal PluginMetadata? DiTailsMetadata { get; }

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            _harmony = new Harmony(HarmonyId);

            var pluginConfig = config.Generated<PluginConfig>();
            PluginConfig.Instance = pluginConfig;

            BeatSaverVotingInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == BeatSaverVotingId);
            MultiplayerInfoInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == MultiplayerInfoId);
            SSMetadata = PluginManager.GetPluginFromId(ScoreSaberId);
            BLMetadata = PluginManager.GetPluginFromId(BeatLeaderId);
            DiTailsMetadata =  PluginManager.GetPluginFromId(DiTailsId);

            DebugLog($"BeatSaverVoting installed: {BeatSaverVotingInstalled}");
            DebugLog($"MultiplayerInfo installed: {MultiplayerInfoInstalled}");
            DebugLog($"ScoreSaber: {SSMetadata?.HVersion.ToString() ?? "not installed"}");
            DebugLog($"BeatLeader: {BLMetadata?.HVersion.ToString() ?? "not installed"}");
            DebugLog($"DiTails: {DiTailsMetadata?.HVersion.ToString() ?? "not installed"}");

            zenjector.UseLogger();
            zenjector.Install<SongPlayTrackingInstaller>(Location.MultiPlayer | Location.StandardPlayer);
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<AppInstaller>(Location.App, pluginConfig);

            Log.Info("Plugin initialized");
        }

        [OnStart]
        public void OnStart()
        {
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnExit]
        public void OnExit()
        {
            _harmony.UnpatchSelf();
        }

        [Conditional("DEBUG")]
        internal static void DebugLog(string message)
        {
            Log.Debug(message);
        }
    }
}
