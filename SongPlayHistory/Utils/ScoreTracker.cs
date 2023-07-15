using System;
using SiraUtil.Logging;
using Zenject;

namespace SongPlayHistory.Utils
{
    public class ScoreTracker : IInitializable, IDisposable
    {
        public static int? MaxRawScore { get; internal set; } = null;
        public static int? RawScore { get; internal set; } = null;

        public static int? MultipliedScore { get; internal set; } = null;
        public static bool EnergyDidReach0 { get; internal set; } = false;
        public static int NotesPassed { get; private set; } = 0;
        
        [InjectOptional]
        private readonly IScoreController? _scoreController = null;

        [InjectOptional]
        private readonly IGameEnergyCounter? _energyCounter = null;

        [InjectOptional]
        private readonly BeatmapObjectManager? _beatmapObjectManager = null;

        [Inject]
        private readonly SiraLog _logger = null!;

        public void Initialize()
        {
            MaxRawScore = null;
            RawScore = null;
            MultipliedScore = null;
            EnergyDidReach0 = false;
            NotesPassed = 0;
            if (_scoreController != null && _energyCounter != null && _beatmapObjectManager != null)
            {
                _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
                _energyCounter.gameEnergyDidReach0Event += OnEnergyDidReach0;
                _scoreController.scoringForNoteFinishedEvent -= OnScoreChanged;
                _scoreController.scoringForNoteFinishedEvent += OnScoreChanged;
                _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
                _beatmapObjectManager.noteWasCutEvent += OnNoteCut;
                _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
                _beatmapObjectManager.noteWasMissedEvent += OnNoteMiss;
            } 
            else 
            {
                _logger.Warn("scoreController or energyCounter or beatmapObjectManager is null!");
            }
        }

        private void OnNoteCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            NotesPassed++;
        }

        private void OnNoteMiss(NoteController noteController)
        {
            NotesPassed++;
        }

        private void OnEnergyDidReach0()
        {
            EnergyDidReach0 = true;
            MaxRawScore = _scoreController?.immediateMaxPossibleMultipliedScore;
            RawScore = _scoreController?.multipliedScore;
            _logger.Info($"Energy reached 0! Notes fired: {NotesPassed} Scores w/o modifiers: {RawScore}/{MaxRawScore}");
        }

        private void OnScoreChanged(ScoringElement _)
        {
            if (!EnergyDidReach0)
            {
                MaxRawScore = _scoreController?.immediateMaxPossibleMultipliedScore;
                RawScore = _scoreController?.multipliedScore;
            }
        }

        public void Dispose()
        {
            if (_energyCounter != null)
            {
                _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
            }
            
            if (_scoreController != null)
            {
                _scoreController.scoringForNoteFinishedEvent -= OnScoreChanged;
            }

            if (_beatmapObjectManager != null)
            {
                _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
                _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
            }
        }
    }
}