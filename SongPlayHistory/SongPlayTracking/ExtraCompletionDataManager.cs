using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace SongPlayHistory.SongPlayTracking;

public class ExtraCompletionDataManager
{
    private readonly ConditionalWeakTable<LevelCompletionResults, LevelCompletionResultsExtraData> _resultTable = new ConditionalWeakTable<LevelCompletionResults, LevelCompletionResultsExtraData>();
    
    internal void AddExtraData(LevelCompletionResults results, LevelCompletionResultsExtraData extraData)
    {
        _resultTable.Add(results, extraData);
    }
    
    [PublicAPI]
    public LevelCompletionResultsExtraData? GetExtraData(LevelCompletionResults results)
    {
        return _resultTable.TryGetValue(results, out var extraData) ? extraData : null;
    }
}