using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace SongPlayHistory.Model
{
    internal class Record
    {
        public long Date = 0L;
        public int ModifiedScore = 0;
        public int RawScore = 0;
        public int LastNote = 0;
        public int Param = 0;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? MaxRawScore = null;
        [JsonIgnore]
        public int? CalculatedMaxRawScore = null;  // only save in memory
        public string ToShortString()
        {
            return $"ModifiedScore {ModifiedScore}, RawScore {RawScore}, LastNote {LastNote}, MaxRawScore {MaxRawScore ?? CalculatedMaxRawScore}";
        }
    }
    
    [Flags]
    internal enum Param
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
        internal static Param ModsToParam(GameplayModifiers mods, bool softFailed)
        {
            Param param = Param.None;
            param |= mods.energyType == GameplayModifiers.EnergyType.Battery ? Param.BatteryEnergy : 0;
            param |= mods.noFailOn0Energy && softFailed ? Param.NoFail : 0;
            param |= mods.instaFail ? Param.InstaFail : 0;
            param |= mods.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles ? Param.NoObstacles : 0;
            param |= mods.noBombs ? Param.NoBombs : 0;
            param |= mods.fastNotes ? Param.FastNotes : 0;
            param |= mods.strictAngles ? Param.StrictAngles : 0;
            param |= mods.disappearingArrows ? Param.DisappearingArrows : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.SuperFast ? Param.SuperFastSong : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.Faster ? Param.FasterSong : 0;
            param |= mods.songSpeed == GameplayModifiers.SongSpeed.Slower ? Param.SlowerSong : 0;
            param |= mods.noArrows ? Param.NoArrows : 0;
            param |= mods.ghostNotes ? Param.GhostNotes : 0;
            param |= mods.smallCubes ? Param.SmallCubes : 0;
            param |= mods.proMode ? Param.ProMode : 0;
            return param;
        } 
        
        internal static string ToParamString(this Param param)
        {
            if (param == Param.None)
            {
                return "";
            }

            var mods = new List<string>(10); // an init capacity of 10 should be plenty in most cases

            if (param.HasFlag(Param.Multiplayer)) mods.Add("MULTI");
            if (param.HasFlag(Param.BatteryEnergy)) mods.Add("BE");
            if (param.HasFlag(Param.NoFail)) mods.Add("NF");
            if (param.HasFlag(Param.InstaFail)) mods.Add("IF");
            if (param.HasFlag(Param.NoObstacles)) mods.Add("NO");
            if (param.HasFlag(Param.NoBombs)) mods.Add("NB");
            if (param.HasFlag(Param.FastNotes)) mods.Add("FN");
            if (param.HasFlag(Param.StrictAngles)) mods.Add("SA");
            if (param.HasFlag(Param.DisappearingArrows)) mods.Add("DA");
            if (param.HasFlag(Param.SuperFastSong)) mods.Add("SFS");
            if (param.HasFlag(Param.FasterSong)) mods.Add("FS");
            if (param.HasFlag(Param.SlowerSong)) mods.Add("SS");
            if (param.HasFlag(Param.NoArrows)) mods.Add("NA");
            if (param.HasFlag(Param.GhostNotes)) mods.Add("GN");
            if (param.HasFlag(Param.SmallCubes)) mods.Add("SN");
            if (param.HasFlag(Param.ProMode)) mods.Add("PRO");
            if (param.HasFlag(Param.SubmissionDisabled)) mods.Add("??");
            if (mods.Count > 4)
            {
                mods = mods.Take(3).ToList(); // Truncate
                mods.Add("..");
            }

            return string.Join(",", mods);
        }
    }
}