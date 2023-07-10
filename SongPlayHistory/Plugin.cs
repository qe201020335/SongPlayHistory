using System;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Logging;
using SiraUtil.Zenject;
using SongPlayHistory.Configuration;
using SongPlayHistory.Installers;
using SongPlayHistory.UI;
using SongPlayHistory.Utils;
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
        private bool _isPractice;
        private bool _isReplay;

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            _harmony = new Harmony(HarmonyId);

            PluginConfig.Instance = config.Generated<PluginConfig>();
            BSMLSettings.instance.AddSettingsMenu("Song Play History", "SongPlayHistory.UI.Settings.bsml", SettingsController.instance);

            RecordsManager.InitializeRecords();
            zenjector.UseLogger();
            zenjector.Install<ScoreTrackerInstaller>(Location.Player);
            zenjector.Install<MenuInstaller>(Location.Menu);
        }

        [OnStart]
        public void OnStart()
        {
            BSEvents.gameSceneLoaded += OnGameSceneLoaded;
            BSEvents.LevelFinished += OnLevelFinished;
            ApplyHarmonyPatches(PluginConfig.Instance.ShowVotes);
        }

        [OnExit]
        public void OnExit()
        {
            BSEvents.gameSceneLoaded -= OnGameSceneLoaded;
            BSEvents.LevelFinished -= OnLevelFinished;

            RecordsManager.BackupRecords();
        }

        private void OnGameSceneLoaded()
        {
            var practiceSettings = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData?.practiceSettings;
            _isPractice = practiceSettings != null;
            _isReplay = Utils.Utils.IsInReplay();
            ScoreTracker.MaxRawScore = null;
        }

        private void OnLevelFinished(object scene, LevelFinishedEventArgs eventArgs)
        {
            if (_isReplay)
            {
                Log.Info("It was a replay, ignored.");
                return;
            }
            
            if (eventArgs.LevelType != LevelType.Multiplayer && eventArgs.LevelType != LevelType.SoloParty)
            {
                return;
            }

            var result = ((LevelFinishedWithResultsEventArgs)eventArgs).CompletionResults;
            
            if (eventArgs.LevelType == LevelType.Multiplayer)
            {
                var beatmap = ((MultiplayerLevelScenesTransitionSetupDataSO)scene).difficultyBeatmap;
                SaveRecord(beatmap, result, true);
            }
            else
            {
                // solo
                if (_isPractice || Gamemode.IsPartyActive)
                {
                    Log.Info("It was in practice or party mode, ignored.");
                    return;
                }
                var beatmap = ((StandardLevelScenesTransitionSetupDataSO)scene).difficultyBeatmap;
                SaveRecord(beatmap, result, false);
            }
            
        }

        private void SaveRecord(IDifficultyBeatmap? beatmap, LevelCompletionResults? result, bool isMultiplayer)
        {
            if (result?.multipliedScore > 0)
            {
                // Actually there's no way to know if any custom modifier was applied if the user failed a map.
                var submissionDisabled = ScoreSubmission.WasDisabled || ScoreSubmission.Disabled || ScoreSubmission.ProlongedDisabled;
                RecordsManager.SaveRecord(beatmap, ScoreTracker.MaxRawScore, result, submissionDisabled, isMultiplayer);
            }
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
