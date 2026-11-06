using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using SiraUtil.Zenject;
using SongPlayHistory.Configuration;
using SongPlayHistory.Installers;
using Config = IPA.Config.Config;

namespace SongPlayHistory
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private const string HarmonyId = "com.github.qe201020335.SongPlayHistory";
        private const string MappingExtensionsId = "MappingExtensions";
        private const string NoodleExtensionsId = "NoodleExtensions";
        private const string ScoreSaberId = "ScoreSaber";
        private const string BeatLeaderId = "BeatLeader";
        
        public static Plugin Instance { get; private set; } = null!;
        internal static Logger Log { get; private set; } = null!;
        
        private readonly Harmony _harmony;

        internal bool MappingExtensionsInstalled { get; }
        internal bool NoodleExtensionsInstalled { get; }

        internal PluginMetadata? SSMetadata { get; }
        internal PluginMetadata? BLMetadata { get; }

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            _harmony = new Harmony(HarmonyId);

            var pluginConfig = config.Generated<PluginConfig>();
            PluginConfig.Instance = pluginConfig;

            MappingExtensionsInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == MappingExtensionsId);
            NoodleExtensionsInstalled = PluginManager.EnabledPlugins.Any(metadata => metadata.Id == NoodleExtensionsId);
            SSMetadata = PluginManager.GetPluginFromId(ScoreSaberId);
            BLMetadata = PluginManager.GetPluginFromId(BeatLeaderId);

            DebugLog($"ScoreSaber: {SSMetadata?.HVersion.ToString() ?? "not installed"}");
            DebugLog($"BeatLeader: {BLMetadata?.HVersion.ToString() ?? "not installed"}");

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
