using System.Linq;
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace SongPlayHistory
{
    internal static class BeatSaberUI
    {
        public static ResultsViewController ResultsViewController { get; private set; }

        public static LevelStatsView LeaderboardLevelStatsView { get; private set; }

        public static StandardLevelDetailViewController LevelDetailViewController { get; private set; }

        public static LevelParamsPanel LevelParamsPanel { get; private set; }

        public static LevelCollectionTableView LevelCollectionTableView { get; private set; }

        private static LevelSelectionFlowCoordinator _flowCoordinator;

        public static bool IsValid => _flowCoordinator != null;

        public static bool IsSolo
        {
            get
            {
                return _flowCoordinator == null || _flowCoordinator is SoloFreePlayFlowCoordinator;
            }
            set
            {
                if (value)
                {
                    _flowCoordinator = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().LastOrDefault();
                    
                    ResultsViewController = (_flowCoordinator as SoloFreePlayFlowCoordinator)?.GetField<ResultsViewController, SoloFreePlayFlowCoordinator>("_resultsViewController");
                    var leaderboardViewController = (_flowCoordinator as SoloFreePlayFlowCoordinator)?.GetField<PlatformLeaderboardViewController, SoloFreePlayFlowCoordinator>("_platformLeaderboardViewController");
                    LeaderboardLevelStatsView = leaderboardViewController?.GetField<LevelStatsView, PlatformLeaderboardViewController>("_levelStatsView");
                }
                else
                {
                    var parent = Resources.FindObjectsOfTypeAll<GameServerLobbyFlowCoordinator>().LastOrDefault();
                    _flowCoordinator = parent?.GetField<MultiplayerLevelSelectionFlowCoordinator, GameServerLobbyFlowCoordinator>("_multiplayerLevelSelectionFlowCoordinator");
                }

                var levelSelectionNavController = _flowCoordinator?.GetField<LevelSelectionNavigationController, LevelSelectionFlowCoordinator>("levelSelectionNavigationController");
                var levelCollectionNavController = levelSelectionNavController?.GetField<LevelCollectionNavigationController, LevelSelectionNavigationController>("_levelCollectionNavigationController");
                LevelDetailViewController = levelCollectionNavController?.GetField<StandardLevelDetailViewController, LevelCollectionNavigationController>("_levelDetailViewController");
                var levelDetailView = LevelDetailViewController?.GetField<StandardLevelDetailView, StandardLevelDetailViewController>("_standardLevelDetailView");
                LevelParamsPanel = levelDetailView?.GetField<LevelParamsPanel, StandardLevelDetailView>("_levelParamsPanel");
                var levelCollectionViewController = levelCollectionNavController?.GetField<LevelCollectionViewController, LevelCollectionNavigationController>("_levelCollectionViewController");
                LevelCollectionTableView = levelCollectionViewController?.GetField<LevelCollectionTableView, LevelCollectionViewController>("_levelCollectionTableView");
            }
        }

        public static void ReloadSongList()
        {
            if (IsValid)
            {
                LevelCollectionTableView?.GetField<TableView, LevelCollectionTableView>("_tableView")?.RefreshCellsContent();
            }
        }
    }
}
