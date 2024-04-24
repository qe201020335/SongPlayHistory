using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SongPlayHistory.Model;
using Zenject;

namespace SongPlayHistory.VoteTracker
{
    internal class InternalVoteTracker: IVoteTracker, IInitializable, IDisposable
    {

        private static readonly string VoteFile = Path.Combine(UnityGame.UserDataPath, "votedSongs.json");
        
        private static Dictionary<string, UserVote>? Votes { get; set; } = new Dictionary<string, UserVote>();

        private static readonly object _voteWriteLock = new();

        [Inject]
        private readonly SiraLog _logger = null!;

        private bool _readonly = true;

        // private DateTime _voteLastWritten;

        public void Initialize()
        {
            _logger.Info("Loading votes.");
            _readonly = true;
            
            if (!File.Exists(VoteFile))
            {
                _logger.Debug("BeatSaverVoting votedSongs.json doesn't exist.");
                _readonly = false;
                return;
            }

            try
            {
                var text = File.ReadAllText(VoteFile, Encoding.UTF8);
                Votes = JsonConvert.DeserializeObject<Dictionary<string, UserVote>?>(text) ?? new Dictionary<string, UserVote>();
                _logger.Info("votedSongs.json Loaded");
            }
            catch (Exception ex) // IOException, JsonException
            {
                _readonly = true;
                _logger.Error("Failed to load votedSongs.json. Entering readonly mode.");
                _logger.Error(ex);
            }

            try
            {
                // backup in case something bad happened.
                File.Copy(VoteFile, VoteFile + ".sph.bak", true);
                _readonly = false;
            }
            catch (Exception e)
            {
                _readonly = true;
                _logger.Error("Failed to backup votedSongs.json. Entering readonly mode.");
                _logger.Error(e);
            }
        }

        private void SaveVotes()
        {
            if (_readonly)
            {
                _logger.Debug("Votes not saved, read only");
                return;
            }

            try
            {
                lock (_voteWriteLock)
                {
                    if (Votes != null && Votes.Count > 0)
                    {
                        File.WriteAllText(VoteFile, JsonConvert.SerializeObject(Votes), Encoding.UTF8);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to save votedSongs.json: {e.Message}");
                _logger.Error(e);
            }
        }

        public void Dispose()
        {
            lock (_voteWriteLock)
            {
                SaveVotes();
                Votes = null;
            }
        }

        public bool TryGetVote(BeatmapLevel level, out VoteType voteType)
        {
            voteType = VoteType.Downvote;
            try
            {
                var hash = Utils.Utils.GetCustomLevelHash(level);
                if (hash != null && Votes?.TryGetValue(hash.ToLower(), out var vote) == true)
                {
                    voteType = vote.VoteType;
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to get vote: {e.Message}");
                _logger.Warn(e);
            }

            return false;
        }

        public void Vote(BeatmapLevel level, VoteType voteType)
        {
            Task.Run(() =>
            {
                lock (_voteWriteLock)
                {
                    var hash = Utils.Utils.GetCustomLevelHash(level);
                    if (hash != null && Votes != null)
                    {
                        hash = hash.ToLower();
                        if (!Votes.ContainsKey(hash) || Votes[hash].VoteType != voteType)
                        {
                            Votes[hash] = new UserVote
                            {
                                Hash = hash,
                                VoteType = voteType
                            };
                            SaveVotes();
                        }
                        Plugin.Log.Info($"Voted {voteType} to {level.levelID}");
                    }
                }
            });
        }

    }
}