using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SongPlayHistory.SongPlayData;

namespace SongPlayHistory.Model
{
    
    [JsonObject(MemberSerialization.OptIn)]
    internal class Record: ISongPlayRecord
    {
        [JsonProperty("Date")]
        public long Date { get; internal set; } = 0L;
        
        [JsonProperty("ModifiedScore")]
        public int ModifiedScore { get; internal set; } = 0;
        
        [JsonProperty("RawScore")]
        public int RawScore { get; internal set; } = 0;
        
        [JsonProperty("LastNote")]
        public int LastNote { get; internal set; } = 0;

        [JsonProperty("Param", DefaultValueHandling = DefaultValueHandling.Ignore)] 
        [DefaultValue(SongPlayParam.None)]
        public SongPlayParam Params { get; internal set; } = SongPlayParam.None;
        
        [JsonProperty("MaxRawScore", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? MaxRawScore { get; internal set; } = null;
        
        public DateTime LocalTime => DateTimeOffset.FromUnixTimeMilliseconds(Date).LocalDateTime;
        
        public LevelEndType LevelEnd {
            get
            {
                if (LastNote == -1)
                {
                    return LevelEndType.Cleared;
                }

                if (LastNote > 0)
                {
                    return LevelEndType.Failed;
                }
                
                // LastNote == 0 old record (success, fail, or practice)
                // LastNote < -1 unknown
                return LevelEndType.Unknown;
            }
        }
        
        public override string ToString()
        {
            return $"ModifiedScore {ModifiedScore}, RawScore {RawScore}, LastNote {LastNote}, MaxRawScore {MaxRawScore}";
        }
        
        internal ISongPlayRecord Copy()
        {
            return new Record()
            {
                Date = Date,
                ModifiedScore = ModifiedScore,
                RawScore = RawScore,
                LastNote = LastNote,
                Params = Params,
                MaxRawScore = MaxRawScore,
            };
        }
    }
    
    public enum LevelEndType
    {
        Cleared = 0,
        Failed = 1,
        Unknown = 2,
    }
    
    [Flags]
    public enum SongPlayParam
    {
        None = 0,
        BatteryEnergy = 1 << 0,
        NoFail = 1 << 1,
        InstaFail = 1 << 2,
        NoObstacles = 1 << 3,
        NoBombs = 1 << 4,
        FastNotes = 1 << 5,
        StrictAngles = 1 << 6,
        DisappearingArrows = 1 << 7,
        FasterSong = 1 << 8,
        SlowerSong = 1 << 9,
        NoArrows = 1 << 10,
        GhostNotes = 1 << 11,
        SuperFastSong = 1 << 12,
        SmallCubes = 1 << 13,
        ProMode = 1 << 14,
        // where is 1 << 15 (0x8000) ??
        SubmissionDisabled = 1 << 16,
        Multiplayer = 1 << 17,
    }

    internal static class ParamHelper
    {
        internal static SongPlayParam ModsToParam(GameplayModifiers mods, bool softFailed)
        {
            SongPlayParam param = SongPlayParam.None;
            param |= mods.energyType == GameplayModifiers.EnergyType.Battery ? SongPlayParam.BatteryEnergy : 0;
            param |= mods.noFailOn0Energy && softFailed ? SongPlayParam.NoFail : 0;
            param |= mods.instaFail ? SongPlayParam.InstaFail : 0;
            param |= mods.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles ? SongPlayParam.NoObstacles : 0;
            param |= mods.noBombs ? SongPlayParam.NoBombs : 0;
            param |= mods.fastNotes ? SongPlayParam.FastNotes : 0;
            param |= mods.strictAngles ? SongPlayParam.StrictAngles : 0;
            param |= mods.disappearingArrows ? SongPlayParam.DisappearingArrows : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.SuperFast ? SongPlayParam.SuperFastSong : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.Faster ? SongPlayParam.FasterSong : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.Slower ? SongPlayParam.SlowerSong : 0;
            param |= mods.noArrows ? SongPlayParam.NoArrows : 0;
            param |= mods.ghostNotes ? SongPlayParam.GhostNotes : 0;
            param |= mods.smallCubes ? SongPlayParam.SmallCubes : 0;
            param |= mods.proMode ? SongPlayParam.ProMode : 0;
            return param;
        } 
        
        internal static string ToParamString(this SongPlayParam param)
        {
            if (param == SongPlayParam.None)
            {
                return "";
            }

            var mods = new List<string>(10); // an init capacity of 10 should be plenty in most cases

            if (param.HasFlag(SongPlayParam.Multiplayer)) mods.Add("MULTI");
            if (param.HasFlag(SongPlayParam.BatteryEnergy)) mods.Add("BE");
            if (param.HasFlag(SongPlayParam.NoFail)) mods.Add("NF");
            if (param.HasFlag(SongPlayParam.InstaFail)) mods.Add("IF");
            if (param.HasFlag(SongPlayParam.NoObstacles)) mods.Add("NO");
            if (param.HasFlag(SongPlayParam.NoBombs)) mods.Add("NB");
            if (param.HasFlag(SongPlayParam.FastNotes)) mods.Add("FN");
            if (param.HasFlag(SongPlayParam.StrictAngles)) mods.Add("SA");
            if (param.HasFlag(SongPlayParam.DisappearingArrows)) mods.Add("DA");
            if (param.HasFlag(SongPlayParam.SuperFastSong)) mods.Add("SFS");
            if (param.HasFlag(SongPlayParam.FasterSong)) mods.Add("FS");
            if (param.HasFlag(SongPlayParam.SlowerSong)) mods.Add("SS");
            if (param.HasFlag(SongPlayParam.NoArrows)) mods.Add("NA");
            if (param.HasFlag(SongPlayParam.GhostNotes)) mods.Add("GN");
            if (param.HasFlag(SongPlayParam.SmallCubes)) mods.Add("SN");
            if (param.HasFlag(SongPlayParam.ProMode)) mods.Add("PRO");
            if (param.HasFlag(SongPlayParam.SubmissionDisabled)) mods.Add("??");
            if (mods.Count > 4)
            {
                mods = mods.Take(3).ToList(); // Truncate
                mods.Add("..");
            }

            return string.Join(",", mods);
        }
    }
}