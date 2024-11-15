using System.Runtime.CompilerServices;

namespace SongPlayHistory.SongPlayTracking;

public class ExtraCompletionDataManager
{
    private readonly ConditionalWeakTable<LevelCompletionResults, LevelCompletionResultsExtraData> _resultTable = new ConditionalWeakTable<LevelCompletionResults, LevelCompletionResultsExtraData>();
    
    internal void AddExtraData(LevelCompletionResults results, LevelCompletionResultsExtraData extraData)
    {
        _resultTable.Add(results, extraData);
    }
    
    public LevelCompletionResultsExtraData? GetExtraData(LevelCompletionResults results)
    {
        return _resultTable.TryGetValue(results, out var extraData) ? extraData : null;
    }
}