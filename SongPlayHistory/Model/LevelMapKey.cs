using System;
using System.Text.RegularExpressions;

namespace SongPlayHistory.Model
{
    public readonly struct LevelMapKey
    {
        public readonly string LevelId;
        public readonly string CharacteristicName;
        public readonly int Difficulty;
        
        // Old key format:
        // $"{beatmap.level.levelID}___{(int)beatmap.difficulty}___{beatmapCharacteristicName}"
        private static readonly Regex OldKeyRegex = new Regex(@"^(.*)___(\d+)___([a-zA-Z0-9]+)$");
        
        public LevelMapKey(string levelId, string characteristicName, int difficulty)
        {
            LevelId = levelId;
            CharacteristicName = characteristicName;
            Difficulty = difficulty;
        }
        
        public LevelMapKey(string levelId, string characteristicName, BeatmapDifficulty difficulty)
            : this(levelId, characteristicName, (int)difficulty)
        {
        }
        
        public LevelMapKey(IDifficultyBeatmap beatmap)
            : this(beatmap.level.levelID, beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName, beatmap.difficulty)
        {
        }
        
        
        public string ToOldKey()
        {
            return $"{LevelId}___{Difficulty}___{CharacteristicName}";
        }
        
        internal static bool TryGetFromOldKey(string oldKey, out LevelMapKey key)
        {
            try
            {
                var match = OldKeyRegex.Match(oldKey);

                if (!match.Success || match.Groups.Count != 4)
                {
                    key = default;
                    return false;
                }
                
                var groups = match.Groups;
                key = new LevelMapKey(groups[1].Value, groups[3].Value, int.Parse(groups[2].Value));
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.Trace($"failed to parse old key: {oldKey} {e}");
                key = default;
                return false;                
            }
        }
    }
}