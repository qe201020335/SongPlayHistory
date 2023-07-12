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

        internal static UserVoteTracker? Instance;

        private static readonly object _instanceLock = new();
        
        private static readonly string VoteFile = Path.Combine(Environment.CurrentDirectory, "UserData", "votedSongs.json");
        
        private static Dictionary<string, UserVote>? Votes { get; set; } = new Dictionary<string, UserVote>();

        private static readonly object _voteWriteLock = new();

        private DateTime _voteLastWritten;

        public void Initialize()
        {
            lock (_instanceLock)
            {
                Instance = this;
            }
            
            ScanVoteData();
        }

        public void Dispose()
        {
            lock (_instanceLock)
            {
                Instance = this;
            }
            lock (_voteWriteLock)
            {
                Votes = null;
            }
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

        // private void Vote(IPreviewBeatmapLevel level, VoteType voteType)
        // {
        //     Plugin.Log.Debug($"Voted {voteType} to {level.levelID}");
        // }

        internal static void Vote(IPreviewBeatmapLevel level, VoteType voteType)
        {
            lock (_instanceLock)
            {
                Plugin.Log.Debug($"Voted {voteType} to {level.levelID}");
            }
        }

    }
    
    public enum VoteType
    {
        UpVote = 0,
        DownVote = 1
    }
}