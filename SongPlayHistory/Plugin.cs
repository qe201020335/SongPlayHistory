using System;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Logging;
using SiraUtil.Zenject;
using SongPlayHistory.Configuration;
using SongPlayHistory.Installers;
using SongPlayHistory.UI;
using Config = IPA.Config.Config;

namespace SongPlayHistory
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public const string HarmonyId = "com.github.qe201020335.SongPlayHistory";

        public static Plugin Instance { get; private set; } = null!;
        public static Logger Log { get; internal set; } = null!;

        private readonly Harmony _harmony;


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
            ApplyHarmonyPatches(PluginConfig.Instance.ShowVotes);
        }


        public void ApplyHarmonyPatches(bool enabled)
        {
            try
            {
                if (enabled && !Harmony.HasAnyPatches(HarmonyId))
                {
                    Log.Info("Applying Harmony patches...");
                    _harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                else if (!enabled && Harmony.HasAnyPatches(HarmonyId))
                {
                    Log.Info("Removing Harmony patches...");
                    _harmony.UnpatchSelf();

                    SetDataFromLevelAsync.OnUnpatch();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while applying Harmony patches.\n" + ex.ToString());
            }
        }
    }
}
