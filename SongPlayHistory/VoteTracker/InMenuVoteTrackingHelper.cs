using System;
using HMUI;
using IPA.Utilities;
using SongPlayHistory.Model;
using Zenject;

namespace SongPlayHistory.VoteTracker
{
    internal class InMenuVoteTrackingHelper: IInitializable, IDisposable
    {
        internal static InMenuVoteTrackingHelper? Instance { get; private set; }
        
        [Inject] 
        private readonly IVoteTracker _voteTracker = null!;
        
        [Inject]
        private readonly ResultsViewController _resultsViewController = null!;
        
        private readonly TableView _tableView;

        public InMenuVoteTrackingHelper(LevelCollectionViewController levelCollectionViewController)
        {
            var levelCollectionTableView = levelCollectionViewController.GetField<LevelCollectionTableView, LevelCollectionViewController>("_levelCollectionTableView");
            _tableView = levelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
        }

        public void Initialize()
        {
            Instance = this;
            _resultsViewController.continueButtonPressedEvent -= OnPlayResultDismiss;
            _resultsViewController.continueButtonPressedEvent += OnPlayResultDismiss;
        }

        public void Dispose()
        {
            Instance = null;
            _resultsViewController.continueButtonPressedEvent -= OnPlayResultDismiss;
        }
        
        private void OnPlayResultDismiss(ResultsViewController _)
        {
            // The user may have voted on this map.
            _tableView.RefreshCellsContent();
        }

        internal void Vote(BeatmapLevel level, VoteType voteType)
        {
            _voteTracker.Vote(level, voteType);
            _tableView.RefreshCellsContent();
        }
        
        internal bool TryGetVote(BeatmapLevel level, out VoteType voteType)
        {
            return _voteTracker.TryGetVote(level, out voteType);
        }
    }
}