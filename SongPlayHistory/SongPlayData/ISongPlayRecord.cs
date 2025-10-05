using System;
using JetBrains.Annotations;
using SongPlayHistory.Model;

namespace SongPlayHistory.SongPlayData;

[PublicAPI]
public interface ISongPlayRecord
{
    public DateTime LocalTime { get; }

    public int ModifiedScore { get; }

    public int RawScore { get; }

    public int LastNote { get; }

    public SongPlayParam Params { get; }
    
    public LevelEndType LevelEnd { get; }

    public int? MaxRawScore { get; }
}