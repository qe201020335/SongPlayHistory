using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using ModestTree;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SongPlayHistory.Configuration;
using SongPlayHistory.Model;
using SongPlayHistory.Utils;
using Zenject;

namespace SongPlayHistory
{
    internal class RecordsManager: IInitializable, IDisposable
    {
        private readonly string DataFile = Path.Combine(Environment.CurrentDirectory, "UserData", "SongPlayData.json");

        private Dictionary<string, IList<Record>> Records { get; set; } = new Dictionary<string, IList<Record>>();

        [Inject]
        private readonly SiraLog _logger = null!;

        public void Initialize()
        {
            // We don't anymore support migrating old records from a config file.
            if (!File.Exists(DataFile))
            {
                return;
            }

            // Read records from a data file.
            var text = File.ReadAllText(DataFile);
            try
            {
                Records = JsonConvert.DeserializeObject<Dictionary<string, IList<Record>>>(text);
                if (Records == null)
                {
                    throw new JsonReaderException("Unable to deserialize an empty JSON string.");
                }
            }
            catch (JsonException ex)
            {
                // The data file is corrupted.
                _logger.Error($"Failed to load history: {ex.Message}");
                _logger.Error(ex);

                // Try to restore from a backup.
                var backup = new FileInfo(Path.ChangeExtension(DataFile, ".bak"));
                if (backup.Exists && backup.Length > 0)
                {
                    _logger.Notice("Restoring from a backup");
                    text = File.ReadAllText(backup.FullName);

                    Records = JsonConvert.DeserializeObject<Dictionary<string, IList<Record>>>(text);
                    if (Records == null)
                    {
                        // Fail hard to prevent overwriting any previous data or breaking the game.
                        throw new Exception("Failed to restore data.");
                    }
                }
                else
                {
                    // There's nothing more we can try. Overwrite the file.
                    Records = new Dictionary<string, IList<Record>>();
                }
                SaveRecordsToFile();
            }
            
            // _logger.Info("Cleaning up passed NF records");
            // foreach (var record in Records.Values.SelectMany(i => i))
            // {
            //     if (record.LastNote < 0 && ((Param)record.Param).HasFlag(Param.NoFail))
            //     {
            //         // if the level is cleared but has the NF flag, remove NF
            //         record.Param = (int) ((Param)record.Param & ~Param.NoFail);
            //     } 
            // } // This will affect all old NF records.
            
            
            // TODO remove bad records?
            
            SaveRecordsToFile();

            BSEvents.gameSceneLoaded -= OnGameSceneLoaded;
            BSEvents.gameSceneLoaded += OnGameSceneLoaded;
            BSEvents.LevelFinished -= OnLevelFinished;
            BSEvents.LevelFinished += OnLevelFinished;
        }

        public void Dispose()
        {
            BSEvents.gameSceneLoaded -= OnGameSceneLoaded;
            BSEvents.LevelFinished -= OnLevelFinished;
            SaveRecordsToFile();
            BackupRecords();
        }
        
        private bool _isPractice;
        private bool _isReplay;
        
        private void OnGameSceneLoaded()
        {
            var practiceSettings = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData?.practiceSettings;
            _isPractice = practiceSettings != null;
            _isReplay = Utils.Utils.IsInReplay();
            ScoreTracker.EnergyDidReach0 = false;
            ScoreTracker.FailScoreRecord = null;
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
            var energyDidReach0 = ScoreTracker.EnergyDidReach0;
            var failRecord = ScoreTracker.FailScoreRecord;
            
            if (eventArgs.LevelType == LevelType.Multiplayer)
            {
                var beatmap = ((MultiplayerLevelScenesTransitionSetupDataSO)scene).difficultyBeatmap;
                SaveRecord(beatmap, result, true, energyDidReach0, failRecord);
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
                SaveRecord(beatmap, result, false, energyDidReach0, failRecord);
            }
        }

        public IList<Record> GetRecords(IDifficultyBeatmap beatmap)
        {
            var config = PluginConfig.Instance;
            var beatmapCharacteristicName = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            var difficulty = $"{beatmap.level.levelID}___{(int)beatmap.difficulty}___{beatmapCharacteristicName}";

            if (Records.TryGetValue(difficulty, out IList<Record> records))
            {
                // LastNote = -1 (cleared), 0 (undefined), n (failed)
                var filtered = config.ShowFailed ? records : records.Where(s => s.LastNote <= 0);
                var ordered = filtered.OrderByDescending(s => config.SortByDate ? s.Date : s.ModifiedScore);
                return ordered.ToList();
            }

            return new List<Record>();
        }

        private void SaveRecord(IDifficultyBeatmap? beatmap, LevelCompletionResults? result, bool isMultiplayer, bool energyDidReach0, ScoreRecord? failRecord)
        {
            if (beatmap == null || result == null)
            {
                _logger.Warn("Beatmap or completionResults is null.");
                return;
            }
            
            if (result.multipliedScore <= 0)
            {
                _logger.Warn("Record ignored, score is 0.");
                return;
            }

            // Cancelled.
            if (result.levelEndStateType == LevelCompletionResults.LevelEndStateType.Incomplete)
            {
                return;
            }

            // We now keep failed records.
            var cleared = result.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
            var submissionDisabled = ScoreSubmission.WasDisabled || ScoreSubmission.Disabled || ScoreSubmission.ProlongedDisabled;
            var noFailEnabled = result.gameplayModifiers.noFailOn0Energy;
            
            _logger.Debug($"Cleared: {cleared}, NoFail: {noFailEnabled}, SoftFailed: {energyDidReach0}, FailRecord: {failRecord}");
            
            var param = ParamHelper.ModsToParam(result.gameplayModifiers, energyDidReach0);
            param |= submissionDisabled ? Param.SubmissionDisabled : 0;
            param |= isMultiplayer ? Param.Multiplayer : 0;
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Record record;
            
            if (cleared && energyDidReach0 && failRecord != null)
            {
                // use our tracked values at the time of soft fail
                record = new Record
                {
                    Date = time,
                    ModifiedScore = failRecord.Value.RawScore,
                    RawScore = failRecord.Value.ModifiedScore,
                    LastNote = failRecord.Value.NotesPassed,
                    Param = (int) param,
                    MaxRawScore = failRecord.Value.MaxRawScore
                };
            }
            else if (!cleared && noFailEnabled && energyDidReach0 && failRecord != null)
            {
                // No fail is enabled, but level still failed (for example, thr FailButton mod)
                record = new Record
                {
                    Date = time,
                    ModifiedScore = failRecord.Value.RawScore,
                    RawScore = failRecord.Value.ModifiedScore,
                    LastNote = failRecord.Value.NotesPassed,
                    Param = (int) param,
                    MaxRawScore = failRecord.Value.MaxRawScore
                };
            }
            else
            {
                record = new Record
                {
                    Date = time,
                    ModifiedScore = result.modifiedScore,
                    RawScore = result.multipliedScore,
                    LastNote = cleared ? -1 : result.goodCutsCount + result.badCutsCount + result.missedCount,
                    Param = (int) param,
                    MaxRawScore = cleared ? null : failRecord?.MaxRawScore
                };
            }

            _logger.Info($"Saving result. Record: {record.ToShortString()}");

            var beatmapCharacteristicName = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            var difficulty = $"{beatmap.level.levelID}___{(int)beatmap.difficulty}___{beatmapCharacteristicName}";

            if (!Records.ContainsKey(difficulty))
            {
                Records.Add(difficulty, new List<Record>());
            }
            Records[difficulty].Add(record);

            // Save to a file. We do this synchronously because the overhead is small. (400 ms / 15 MB, 60 ms / 1 MB)
            SaveRecordsToFile();

            _logger.Info($"Saved a new record {difficulty} ({result.modifiedScore}).");
        }

        private void SaveRecordsToFile()
        {
            try
            {
                if (Records.Count > 0)
                {
                    var serialized = JsonConvert.SerializeObject(Records, Formatting.Indented);
                    File.WriteAllText(DataFile, serialized);
                }
            }
            catch (Exception ex) // IOException, JsonException
            {
                _logger.Error(ex.ToString());
            }
        }

        private void BackupRecords()
        {
            if (!File.Exists(DataFile))
            {
                return;
            }

            var backupFile = Path.ChangeExtension(DataFile, ".bak");
            try
            {
                if (File.Exists(backupFile))
                {
                    // Compare file sizes instead of the last modified.
                    if (new FileInfo(DataFile).Length > new FileInfo(backupFile).Length)
                    {
                        File.Copy(DataFile, backupFile, true);
                    }
                    else
                    {
                        _logger.Info("Nothing to backup.");
                    }
                }
                else
                {
                    File.Copy(DataFile, backupFile);
                }
            }
            catch (IOException ex)
            {
                _logger.Error(ex.ToString());
            }
        }
    }
}
