using System;
using System.Text.RegularExpressions;

namespace SongPlayHistory.Model
{
    public readonly struct LevelMapKey
    {
        public readonly string LevelId;
        public readonly string CharacteristicName;
        public readonly BeatmapDifficulty Difficulty;

        // Old key format:
        // $"{beatmap.level.levelID}___{(int)beatmap.difficulty}___{beatmapCharacteristicName}"

        internal LevelMapKey(string levelId, string characteristicName, BeatmapDifficulty difficulty)
        {
            LevelId = levelId;
            CharacteristicName = characteristicName;
            Difficulty = difficulty;
        }

        public LevelMapKey(BeatmapKey beatmap)
            : this(beatmap.levelId, beatmap.beatmapCharacteristic.serializedName, beatmap.difficulty)
        {
        }

        public override string ToString()
        {
            return $"{LevelId}:{CharacteristicName}:{Difficulty}";
        }
        
        private static int DifficultyToInt(BeatmapDifficulty difficulty)
        {
            switch (difficulty)
            {
                // in case beat game does something weird, like what they did with light ids
                case BeatmapDifficulty.Easy:
                    return 0;
                case BeatmapDifficulty.Normal:
                    return 1;
                case BeatmapDifficulty.Hard:
                    return 2;
                case BeatmapDifficulty.Expert:
                    return 3;
                case BeatmapDifficulty.ExpertPlus:
                    return 4;
                default:
                    return (int) difficulty;
            }
        }

        internal string ToOldKey()
        {
            return $"{LevelId}___{DifficultyToInt(Difficulty)}___{CharacteristicName}";
        }
    }
}