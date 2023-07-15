using System;
using SiraUtil.Logging;
using Zenject;

namespace SongPlayHistory.Utils
{
    public class ScoreTracker : IInitializable, IDisposable
    {
        private int _maxRawScore;
        private int _rawScore;
        private int _modifiedScore;
        private int _notesPassed;

        public static bool EnergyDidReach0 { get; internal set; } = false;

        /**
         * Score record of when the energy first reached 0
         */
        public static ScoreRecord? FailScoreRecord { get; internal set; } = null;
        
        [Inject]
        private readonly IScoreController _scoreController = null!;

        [Inject]
        private readonly IGameEnergyCounter _energyCounter = null!;

        [Inject]
        private readonly BeatmapObjectManager _beatmapObjectManager = null!;

        [Inject]
        private readonly SiraLog _logger = null!;

        public void Initialize()
        {
            _maxRawScore = 0;
            _rawScore = 0;
            _modifiedScore = 0;
            EnergyDidReach0 = false;
            _notesPassed = 0;
            
            _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
            _energyCounter.gameEnergyDidReach0Event += OnEnergyDidReach0;
            _scoreController.scoringForNoteFinishedEvent -= OnScoreChanged;
            _scoreController.scoringForNoteFinishedEvent += OnScoreChanged;
            _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
            _beatmapObjectManager.noteWasCutEvent += OnNoteCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
            _beatmapObjectManager.noteWasMissedEvent += OnNoteMiss;
        }

        private void OnNoteCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            _notesPassed++;
        }

        private void OnNoteMiss(NoteController noteController)
        {
            _notesPassed++;
        }

        private void OnEnergyDidReach0()
        {
            if (EnergyDidReach0) return;
            
            EnergyDidReach0 = true;
            _maxRawScore = _scoreController.immediateMaxPossibleMultipliedScore;
            _modifiedScore = _scoreController.modifiedScore;
            _rawScore = _scoreController.multipliedScore;
            _logger.Info($"Energy reached 0! Notes fired: {_notesPassed}, Scores: {_rawScore}/{_modifiedScore}/{_maxRawScore}");
            
            FailScoreRecord = new ScoreRecord(
                energyDidReach0:true, 
                rawScore:_rawScore, 
                modifiedScore:_modifiedScore, 
                maxRawScore:_maxRawScore, 
                notesPassed:_notesPassed);
        }

        private void OnScoreChanged(ScoringElement _)
        {
            _maxRawScore = _scoreController.immediateMaxPossibleMultipliedScore;
            _modifiedScore = _scoreController.modifiedScore;
            _rawScore = _scoreController.multipliedScore;
        }

        public void Dispose()
        {
            _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
            _scoreController.scoringForNoteFinishedEvent -= OnScoreChanged;
            _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
        }
    }

    public struct ScoreRecord
    {
        public readonly bool EnergyDidReach0;
        public readonly int MaxRawScore;
        public readonly int RawScore;
        public readonly int ModifiedScore;
        public readonly int NotesPassed;

        internal ScoreRecord(bool energyDidReach0, int rawScore, int modifiedScore, int maxRawScore, int notesPassed)
        {
            EnergyDidReach0 = energyDidReach0;
            RawScore = rawScore;
            ModifiedScore = modifiedScore;
            MaxRawScore = maxRawScore;
            NotesPassed = notesPassed;
        }

        public override string ToString()
        {
            return $"EnergyDidReach0 {EnergyDidReach0}, Notes fired: {NotesPassed}, Scores: {RawScore}/{ModifiedScore}/{MaxRawScore}";
        }
    }
}