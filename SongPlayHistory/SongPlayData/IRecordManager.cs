using System;
using System.Collections.Generic;
using JetBrains.Annotations;


namespace SongPlayHistory.SongPlayData;

[PublicAPI]
public interface IRecordManager
{
    public IList<ISongPlayRecord> GetRecords(BeatmapKey beatmap);
}