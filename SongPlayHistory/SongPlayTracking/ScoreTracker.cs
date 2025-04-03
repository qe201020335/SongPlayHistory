using System;
using SiraUtil.Logging;
using Zenject;

namespace SongPlayHistory.SongPlayTracking
{
    public class ScoreTracker : IInitializable, IDisposable
    {
        [Inject]
        private readonly IScoreController _scoreController = null!;

        [Inject]
        private readonly IGameEnergyCounter _energyCounter = null!;

        [Inject]
        private readonly BeatmapObjectManager _beatmapObjectManager = null!;

        [Inject]
        private readonly SiraLog _logger = null!;
        
        public bool EnergyDidReach0 => FailScoreRecord != null;

        /**
         * Score record of when the energy first reached 0
         */
        internal ScoreRecord? FailScoreRecord { get; private set; } = null;
        
        private int _notesPassed;

        public void Initialize()
        {
            FailScoreRecord = null;
            _notesPassed = 0;
            
            _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
            _energyCounter.gameEnergyDidReach0Event += OnEnergyDidReach0;
            _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
            _beatmapObjectManager.noteWasCutEvent += OnNoteCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
            _beatmapObjectManager.noteWasMissedEvent += OnNoteMiss;
        }

        private void OnNoteCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (noteController.noteData.gameplayType != NoteData.GameplayType.Bomb) _notesPassed++;
        }

        private void OnNoteMiss(NoteController noteController)
        {
            if (noteController.noteData.gameplayType != NoteData.GameplayType.Bomb) _notesPassed++;
        }

        private void OnEnergyDidReach0()
        {
            if (EnergyDidReach0) return;
            
            FailScoreRecord = GetImmediateScoreData();
            _logger.Info($"Energy reached 0! {FailScoreRecord}");
        }

        internal ScoreRecord GetImmediateScoreData()
        {
            return new ScoreRecord(
                rawScore:_scoreController.multipliedScore, 
                modifiedScore:_scoreController.modifiedScore, 
                maxRawScore:_scoreController.immediateMaxPossibleMultipliedScore, 
                notesPassed:_notesPassed);
        }

        public void Dispose()
        {
            _energyCounter.gameEnergyDidReach0Event -= OnEnergyDidReach0;
            _beatmapObjectManager.noteWasCutEvent -= OnNoteCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteMiss;
        }
    }
}