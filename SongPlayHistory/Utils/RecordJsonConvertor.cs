using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SongPlayHistory.Model;

namespace SongPlayHistory.Utils
{
    internal class RecordJsonConvertor : JsonConverter<Dictionary<LevelMapKey, IList<Record>>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<LevelMapKey, IList<Record>>? value, JsonSerializer serializer)
        {
            value ??= new Dictionary<LevelMapKey, IList<Record>>();
            Plugin.Log.Debug($"[RecordConvertor] map to save: {value.Count}");
            var mapped = value.ToDictionary(pair => pair.Key.ToOldKey(), pair => pair.Value);
            serializer.Serialize(writer, mapped);
        }

        public override Dictionary<LevelMapKey, IList<Record>>? ReadJson(JsonReader reader, Type objectType,
            Dictionary<LevelMapKey, IList<Record>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var oldMap = serializer.Deserialize<Dictionary<string, IList<Record>>>(reader) ?? new Dictionary<string, IList<Record>>();

            Plugin.Log.Debug($"[RecordConvertor] oldMap.Count: {oldMap.Count}");
            
            var converted = hasExistingValue && existingValue != null
                ? new Dictionary<LevelMapKey, IList<Record>>(existingValue)
                : new Dictionary<LevelMapKey, IList<Record>>(oldMap.Count);
            
            Plugin.Log.Debug($"[RecordConvertor] existing: {converted.Count}");

            foreach (var pair in oldMap)
            {
                if (LevelMapKey.TryGetFromOldKey(pair.Key, out var key))
                {
                    if (converted.TryGetValue(key, out var records))
                    {
                        converted[key] = records.Concat(pair.Value).ToList();
                    }
                    else
                    {
                        converted[key] = pair.Value;
                    }
                } else {
                    Plugin.Log.Warn($"[RecordConvertor] failed to parse key: {pair.Key}");
                }
            }

            Plugin.Log.Debug($"[RecordConvertor] converted: {converted.Count}");
            return converted;
        }
    }
}