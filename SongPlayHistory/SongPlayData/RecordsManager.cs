﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using IPA.Utilities;
using ModestTree;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SongPlayHistory.Configuration;
using SongPlayHistory.Model;
using SongPlayHistory.Utils;
using Zenject;

namespace SongPlayHistory.SongPlayData
{
    internal class RecordsManager: IInitializable, IDisposable, IRecordManager
    {
        private readonly string DataFile = Path.Combine(UnityGame.UserDataPath, "SongPlayData.json");

        private Dictionary<string, IList<Record>> Records { get; set; } = new();

        [Inject]
        private readonly SiraLog _logger = null!;

        public void Initialize()
        {
            // We don't anymore support migrating old records from a config file.

            if (!LoadRecords(DataFile, out var records) || records.Count == 0)
            {
                _logger.Warn("Did not load any records from file. Will try to restore from a backup.");
                // Try to restore from a backup.
                var backup = new FileInfo(Path.ChangeExtension(DataFile, ".bak"));
                if (backup.Exists && backup.Length > 0)
                {
                    _logger.Notice("Restoring from a backup");
                    LoadRecords(backup.FullName, out records);
                    Records = records;
                }
                else
                {
                    // There's nothing more we can try. Overwrite the file.
                    _logger.Warn("Backup not found.");
                    Records = new Dictionary<string, IList<Record>>();
                }
            }
            else
            {
                Records = records;
            }
            
            SaveRecordsToFile();
            _logger.Info($"Loaded {SumRecords(Records)} records from {Records.Count} levels.");
            
            // TODO remove bad records?

            BSEvents.gameSceneLoaded -= OnGameSceneLoaded;
            BSEvents.gameSceneLoaded += OnGameSceneLoaded;
            BSEvents.LevelFinished -= OnLevelFinished;
            BSEvents.LevelFinished += OnLevelFinished;
        }

        private bool LoadRecords(string path, out Dictionary<string, IList<Record>> records)
        {
            _logger.Info($"Loading history from {path}");
            records = new Dictionary<string, IList<Record>>();
            
            if (!File.Exists(path))
            {
                _logger.Warn($"History file doesn't exist: {path}");
                return false;
            }
            
            try
            {
                // Read records from a data file.
                var text = File.ReadAllText(path);
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, IList<Record>>>(text);
                records = deserialized ?? throw new Exception();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Unable to deserialize song play records.");
                _logger.Error(e);
                return false;
            }
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
                _logger.Info("It was a replay, ignored.");
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
                var beatmap = ((MultiplayerLevelScenesTransitionSetupDataSO)scene).beatmapKey;
                SaveRecord(beatmap, result, true, energyDidReach0, failRecord);
            }
            else
            {
                // solo
                if (_isPractice || Gamemode.IsPartyActive)
                {
                    _logger.Info("It was in practice or party mode, ignored.");
                    return;
                }
                var beatmap = ((StandardLevelScenesTransitionSetupDataSO)scene).beatmapKey;
                SaveRecord(beatmap, result, false, energyDidReach0, failRecord);
            }
        }

        public IList<ISongPlayRecord> GetRecords(BeatmapKey beatmap)
        {
            var key = new LevelMapKey(beatmap);
            return GetRecords(key);
        }
        
        public IList<ISongPlayRecord> GetRecords(LevelMapKey key)
        {
            _logger.Debug($"Getting records for {key}");
            if (Records.TryGetValue(key.ToOldKey(), out var records))
            {
                _logger.Debug($"Total number of records: {records.Count}");
                return records.Copy();
            }

            _logger.Debug("No records found.");
            return new List<ISongPlayRecord>();
        }

        private void SaveRecord(BeatmapKey? beatmap, LevelCompletionResults? result, bool isMultiplayer, bool energyDidReach0, ScoreRecord? failRecord)
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
            param |= submissionDisabled ? SongPlayParam.SubmissionDisabled : 0;
            param |= isMultiplayer ? SongPlayParam.Multiplayer : 0;
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
                    Params = param,
                    MaxRawScore = failRecord.Value.MaxRawScore
                };
            }
            else if (!cleared && noFailEnabled && energyDidReach0 && failRecord != null)
            {
                // No fail is enabled and did soft fail, but level still failed (for example, the FailButton mod)
                // ues our tracked values at the time of soft fail
                record = new Record
                {
                    Date = time,
                    ModifiedScore = failRecord.Value.RawScore,
                    RawScore = failRecord.Value.ModifiedScore,
                    LastNote = failRecord.Value.NotesPassed,
                    Params = param,
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
                    Params = param,
                    MaxRawScore = cleared ? null : failRecord?.MaxRawScore
                };
            }

            _logger.Info($"Saving result. Record: {record}");

            var key = new LevelMapKey(beatmap.Value).ToOldKey();

            if (!Records.ContainsKey(key))
            {
                Records.Add(key, new List<Record>());
            }
            Records[key].Add(record);

            // Save to a file. We do this synchronously because the overhead is small. (400 ms / 15 MB, 60 ms / 1 MB)
            SaveRecordsToFile();

            _logger.Info($"Saved a new record ({result.modifiedScore}).");
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
                _logger.Error($"Failed to save records to file: {ex.Message}");
                _logger.Error(ex);
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
                if (File.Exists(backupFile) && LoadRecords(backupFile, out var backupRecords))
                {
                    // Compare file sizes instead of the last modified.
                    if (SumRecords(Records) >= SumRecords(backupRecords))
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

        private static int SumRecords(Dictionary<string, IList<Record>> records)
        {
            return records.Select(pair => pair.Value.Count).Sum();
        }
    }
}
