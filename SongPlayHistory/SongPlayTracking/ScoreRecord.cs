namespace SongPlayHistory.SongPlayTracking;

public readonly struct ScoreRecord
{
    public readonly int MaxRawScore;
    public readonly int RawScore;
    public readonly int ModifiedScore;
    public readonly int NotesPassed;

    internal ScoreRecord(int rawScore, int modifiedScore, int maxRawScore, int notesPassed)
    {
        RawScore = rawScore;
        ModifiedScore = modifiedScore;
        MaxRawScore = maxRawScore;
        NotesPassed = notesPassed;
    }

    public override string ToString()
    {
        return $"Notes passed: {NotesPassed}, Scores: {RawScore}/{ModifiedScore}/{MaxRawScore}";
    }
}