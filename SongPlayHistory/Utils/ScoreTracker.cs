using System;
using Zenject;

namespace SongPlayHistory.Utils
{
    public class ScoreTracker : IInitializable, IDisposable
    {
        internal static int? MaxRawScore = null;
        
        [InjectOptional]
        private IScoreController? _scoreController = null;

        public void Initialize()
        {
            MaxRawScore = null;
            if (_scoreController != null)
            {
                _scoreController.scoringForNoteFinishedEvent += OnScoreChanged;
            } 
            else 
            {
                Plugin.Log.Warn("scoreController is null!");
            }
        }

        private void OnScoreChanged(ScoringElement _)
        {
            MaxRawScore = _scoreController?.immediateMaxPossibleMultipliedScore;
        }

        public void Dispose()
        {
            if (_scoreController != null)
            {
                Plugin.Log.Info($"Max possible score w/o modifiers: {MaxRawScore}");
                _scoreController.scoringForNoteFinishedEvent -= OnScoreChanged;
            }
        }
    }
}