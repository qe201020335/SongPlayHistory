using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HMUI;
using IPA.Utilities;
using Polyglot;
using SiraUtil.Logging;
using SongPlayHistory.Configuration;
using SongPlayHistory.Model;
using SongPlayHistory.SongPlayData;
using SongPlayHistory.Utils;
using TMPro;
using UnityEngine;
using Zenject;
using UObject = UnityEngine.Object;

namespace SongPlayHistory.UI
{
    internal class SPHUI: IInitializable, IDisposable
    {
        [Inject]
        private readonly IRecordManager _recordsManager = null!;

        [Inject]
        private readonly PlayerDataModel _playerDataModel = null!;

        [Inject]
        private readonly ResultsViewController _resultsViewController = null!;
        
        private readonly SiraLog _logger = null!;
        
        private readonly StandardLevelDetailViewController _levelDetailViewController;
        
        private readonly HoverHint _hoverHint;

        private readonly TMP_Text _playCount;
        
        private static readonly Dictionary<LevelMapKey, Tuple<int, int, bool>> ScoringCache = new(); // [key: (MaxScore, NoteCount, isV2Score)]

        public SPHUI(PlatformLeaderboardViewController leaderboardViewController, StandardLevelDetailViewController levelDetailViewController,
            HoverHintController hoverHintController, SiraLog logger)
        {
            _levelDetailViewController = levelDetailViewController;
            _logger = logger;
            
            var levelStatsView = leaderboardViewController.GetField<LevelStatsView, PlatformLeaderboardViewController>("_levelStatsView");
            var levelDetailView = levelDetailViewController.GetField<StandardLevelDetailView, StandardLevelDetailViewController>("_standardLevelDetailView");
            var levelParamsPanel = levelDetailView.GetField<LevelParamsPanel, StandardLevelDetailView>("_levelParamsPanel");

            try
            {
                _logger.Info("Preparing SPU UI");
                _hoverHint = PrepareHoverHint((levelStatsView.transform as RectTransform)!, levelParamsPanel, hoverHintController);
                _playCount = PreparePlayCount(levelStatsView);
            }
            catch (Exception ex)
            {
                _logger.Critical($"Failed to prepare SPU UI: {ex.Message}");
                _logger.Error(ex);
            }
        }
        
        private HoverHint PrepareHoverHint(RectTransform parent, LevelParamsPanel levelParamsPanel, HoverHintController hoverHintController)
        {
            _logger.Debug("Preparing hover area for play history");
            var template = levelParamsPanel.GetComponentsInChildren<RectTransform>().First(x => x.name == "NotesCount");
            var label = UObject.Instantiate(template, parent);
            label.name = "SPH HoverArea";
            label.MatchParent();
            UObject.Destroy(label.Find("Icon").gameObject);
            UObject.Destroy(label.Find("ValueText").gameObject);
            UObject.Destroy(label.GetComponentInChildren<HoverHint>());
            UObject.Destroy(label.GetComponentInChildren<LocalizedHoverHint>());

            var hoverHint = label.gameObject.AddComponent<HoverHint>();
            hoverHint.SetField("_hoverHintController", hoverHintController);
            hoverHint.text = "";
            return hoverHint;
        }

        private TMP_Text PreparePlayCount(LevelStatsView levelStatsView)
        {
            _logger.Debug("Preparing extra level stats ui for play count");
            var maxCombo = levelStatsView.GetComponentsInChildren<RectTransform>().First(x => x.name == "MaxCombo");
            var highscore = levelStatsView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Highscore");
            var maxRank = levelStatsView.GetComponentsInChildren<RectTransform>().First(x => x.name == "MaxRank");

            var playCount = UObject.Instantiate(maxCombo, levelStatsView.transform);
            playCount.name = "SPH PlayCount";

            const float w = 0.225f;
            maxCombo.anchorMin = new Vector2(0f, .5f);
            maxCombo.anchorMax = new Vector2(1 * w, .5f);
            highscore.anchorMin = new Vector2(1 * w, .5f);
            highscore.anchorMax = new Vector2(2 * w, .5f);
            maxRank.anchorMin = new Vector2(2 * w, .5f);
            maxRank.anchorMax = new Vector2(3 * w, .5f);
            playCount.anchorMin = new Vector2(3 * w, .5f);
            playCount.anchorMax = new Vector2(4 * w, .5f);
                    
            var title = playCount.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "Title");
            // The text behind the title of the cloned maxCombo component is localized, so we need 
            // to destroy the localized component so that we can change its text to "Play Count", 
            // otherwise the localized text is retained in favour of ours
            UObject.Destroy(title.GetComponentInChildren<LocalizedTextMeshProUGUI>());
            title.text = "Play Count";
            var text = playCount.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "Value");
            return text;
        }
        
        
        public void Initialize()
        {
            _levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
            _levelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
            _levelDetailViewController.didChangeContentEvent -= OnContentChanged;
            _levelDetailViewController.didChangeContentEvent += OnContentChanged;
            _resultsViewController.continueButtonPressedEvent -= OnPlayResultDismiss;
            _resultsViewController.continueButtonPressedEvent += OnPlayResultDismiss;
        }

        public void Dispose()
        {
            _levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
            _levelDetailViewController.didChangeContentEvent -= OnContentChanged;
            _resultsViewController.continueButtonPressedEvent -= OnPlayResultDismiss;
        }
        
        private void OnDifficultyChanged(StandardLevelDetailViewController _, IDifficultyBeatmap beatmap)
        {
            UpdateUI(beatmap);
        }

        private void OnContentChanged(StandardLevelDetailViewController _, StandardLevelDetailViewController.ContentType contentType)
        {
            if (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady)
            {
                UpdateUI(_levelDetailViewController.selectedDifficultyBeatmap);
            }
        }

        private void OnPlayResultDismiss(ResultsViewController _)
        {
            UpdateUI(_levelDetailViewController.selectedDifficultyBeatmap);
        }

        private void UpdateUI(IDifficultyBeatmap? beatmap)
        {
            if (beatmap == null) return;
            _logger.Info("Updating SPH UI");
            _logger.Debug($"{beatmap.level.songName} {beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName} {beatmap.difficulty}");
            try
            {
                SetRecords(beatmap);
                SetStats(beatmap);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to update SPH ui, {ex.GetType().Name}: {ex.Message}");
                _logger.Debug(ex);
            }
        }
        
        private async void SetRecords(IDifficultyBeatmap beatmap)
        {
            var config = PluginConfig.Instance;
            var key = new LevelMapKey(beatmap);
            var records =
                from record in _recordsManager.GetRecords(key)
                where config.ShowFailed || record.LevelEnd == LevelEndType.Cleared
                select record;
            
            records = config.SortByDate 
                ? records.OrderByDescending(record => record.LocalTime) 
                : records.OrderByDescending(record => record.ModifiedScore);

            var truncated = records.Take(10).ToList();
            
            if (truncated.Count == 0)
            {
                _hoverHint.text = "No record";
                return;
            }
            
            int fullMaxScore, notesCount;
            bool isV2Score;

            if (ScoringCache.TryGetValue(key, out var cache))
            {
                (fullMaxScore, notesCount, isV2Score) = cache;
            }
            else
            {
                var beatmapData = await beatmap.GetBeatmapDataAsync(beatmap.GetEnvironmentInfo(), _playerDataModel.playerData.playerSpecificSettings);
                notesCount = beatmapData.cuttableNotesCount;
                fullMaxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(beatmapData);
                // we can use the original v2 scoring method to calculate the adjusted max score if there is no slider or burst
                isV2Score = !beatmapData.GetBeatmapDataItems<SliderData>(0).Any();
                ScoringCache[key] = Tuple.Create(fullMaxScore, notesCount, isV2Score);
            }
            
            var builder = new StringBuilder(200);
            foreach (var r in truncated)
            {
                _logger.Trace($"Record: {r}");
                builder.TMPSpace(truncated.Count - truncated.IndexOf(r) - 1);
                builder.Append($"<size=2.5><color=#1a252bff> {r.LocalTime:d}</color></size>");
                builder.Append($"<size=3.5><color=#0f4c75ff> {r.ModifiedScore}</color></size>");
                
                var levelFinished = r.LevelEnd == LevelEndType.Cleared;

                #region acc
                var denominator = -1;
                if (levelFinished || !config.AverageAccuracy)
                {
                    denominator = fullMaxScore;
                }
                else if (r.MaxRawScore != null)
                {
                    denominator = r.MaxRawScore.Value;
                }
                else if (isV2Score)
                {
                    denominator = ScoreUtils.CalculateV2MaxScore(r.LastNote);
                }
                // Only display acc if we can get the max scores with the data we have on hand.
                // Some soft failed record saved total score instead of at the time of fail   
                // result in the the saved score be much greater than the max score
                if (denominator > 0 && r.RawScore <= denominator)
                {
                    var accuracy = r.RawScore / (float)denominator * 100f;
                    builder.Append($"<size=3.5><color=#368cc6ff> {accuracy:0.00}%</color></size>");
                }
                #endregion

                #region modifiers
                var param = r.Params;
                if (levelFinished && r.ModifiedScore == r.RawScore)
                {
                    // NF penalty definitely not triggered 
                    param &= ~SongPlayParam.NoFail;
                }

                var paramString = param.ToParamString();
                if (paramString.Length == 0 && r.RawScore != r.ModifiedScore)
                {
                    paramString = "?!";
                }
                
                if (paramString.Length > 0)
                {
                    builder.Append($"<size=2><color=#1a252bff> {paramString}</color></size>");
                }
                #endregion
                
                #region level end state
                if (config.ShowFailed)
                {
                    var notesRemaining = notesCount - r.LastNote;
                    if (r.LastNote == -1)
                        builder.Append($"<size=2.5><color=#1a252bff> cleared</color></size>");
                    else if (r.LastNote == 0) // old record (success, fail, or practice)
                        builder.Append($"<size=2.5><color=#584153ff> unknown</color></size>");
                    else
                        builder.Append($"<size=2.5><color=#ff5722ff> +{notesRemaining} notes</color></size>");
                }
                #endregion

                builder.TMPSpace(truncated.IndexOf(r));
                builder.AppendLine();
            }
            
            _hoverHint.text = builder.ToString();
        }

        private void SetStats(IDifficultyBeatmap beatmap)
        {
            var stats = _playerDataModel.playerData.GetPlayerLevelStatsData(beatmap.level.levelID, beatmap.difficulty, beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
            _playCount.text = stats.playCount.ToString();
        }
    }
}