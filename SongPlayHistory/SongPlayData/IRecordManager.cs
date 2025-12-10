using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SongPlayHistory.SongPlayTracking;

namespace SongPlayHistory.SongPlayData;

[PublicAPI]
public interface IRecordManager
{
    public IList<ISongPlayRecord> GetRecords(BeatmapKey beatmap);

    internal void SaveRecord(LevelCompletionResults results, LevelCompletionResultsExtraData extraData);
}