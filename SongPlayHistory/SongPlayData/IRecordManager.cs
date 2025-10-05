using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

[PublicAPI]
public interface IRecordManager
{
    public IList<ISongPlayRecord> GetRecords(BeatmapKey beatmap);
    
    [Obsolete("Use GetRecords(BeatmapKey) instead", true)]
    public IList<ISongPlayRecord> GetRecords(LevelMapKey key);
    
}