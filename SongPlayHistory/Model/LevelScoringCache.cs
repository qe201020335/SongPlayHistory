namespace SongPlayHistory.Model;

public struct LevelScoringCache
{
    public int MaxMultipliedScore;

    public int NotesCount;

    public bool IsV2Score;

    public override string ToString()
    {
        return $"MaxMultipliedScore: {MaxMultipliedScore}, NotesCount: {NotesCount}, IsV2Score: {IsV2Score}";
    }
}