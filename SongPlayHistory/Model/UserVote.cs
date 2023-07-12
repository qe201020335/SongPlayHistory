using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SongPlayHistory.Model
{
    internal struct UserVote
    {
        [JsonProperty("hash")]
        internal string Hash;
        
        [JsonProperty("voteType")]
        [JsonConverter(typeof(StringEnumConverter))]
        internal string VoteType;
    }
    
    public enum VoteType
    {
        Upvote,
        Downvote
    }
}