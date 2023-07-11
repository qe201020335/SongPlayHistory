using System;
using System.Collections.Generic;
using System.IO;
using HMUI;
using IPA.Utilities;
using Newtonsoft.Json;
using SongPlayHistory.Model;
using Zenject;

namespace SongPlayHistory
{
    public class UserVoteTracker: IInitializable, IDisposable
    {
        
        private static readonly string VoteFile = Path.Combine(Environment.CurrentDirectory, "UserData", "votedSongs.json");
        
        private static Dictionary<string, UserVote> Votes { get; set; } = new Dictionary<string, UserVote>();
        
        // private readonly TableView _tableView;
        
        private DateTime _voteLastWritten;


        // public UserVoteTracker(LevelCollectionViewController levelCollectionViewController)
        // {
        //     var levelCollectionTableView = levelCollectionViewController.GetField<LevelCollectionTableView, LevelCollectionViewController>("_levelCollectionTableView");
        //     _tableView = levelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
        // }

        public void Initialize()
        {
            ScanVoteData();
        }

        public void Dispose()
        {
            Votes.Clear();
        }
        
        internal bool ScanVoteData()
        {
            Plugin.Log?.Info($"Scanning {Path.GetFileName(VoteFile)}...");

            if (!File.Exists(VoteFile))
            {
                Plugin.Log?.Warn("The file doesn't exist.");
                return false;
            }
            try
            {
                if (_voteLastWritten != File.GetLastWriteTime(VoteFile))
                {
                    _voteLastWritten = File.GetLastWriteTime(VoteFile);

                    var text = File.ReadAllText(VoteFile);
                    Votes = JsonConvert.DeserializeObject<Dictionary<string, UserVote>>(text) ?? new Dictionary<string, UserVote>();

                    Plugin.Log?.Info("Update done.");
                }

                return true;
            }
            catch (Exception ex) // IOException, JsonException
            {
                Plugin.Log?.Error(ex.ToString());
                return false;
            }
        }
        
        // private void OnPlayResultDismiss(ResultsViewController _)
        // {
        //     // The user may have voted on this map.
        //     ScanVoteData();
        //     _tableView.RefreshCellsContent();
        // }

        internal static bool TryGetVote(IPreviewBeatmapLevel level, out VoteType voteType)
        {
            if (Votes.TryGetValue(level.levelID.Replace("custom_level_", "").ToLower(), out var vote))
            {
                voteType = vote.voteType == "Upvote" ? VoteType.UpVote : VoteType.DownVote;
                return true;
            }

            voteType = VoteType.DownVote;
            return false;
        }

    }
    
    public enum VoteType
    {
        UpVote = 0,
        DownVote = 1
    }
}