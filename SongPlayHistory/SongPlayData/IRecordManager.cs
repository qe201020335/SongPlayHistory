using System.Collections.Generic;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

public interface IRecordManager
{
    public IList<ISongPlayRecord> GetRecords(BeatmapKey beatmap);
    
    public IList<ISongPlayRecord> GetRecords(LevelMapKey key);
    
}