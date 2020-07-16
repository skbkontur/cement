using Newtonsoft.Json;

namespace Common
{
    [JsonObject]
    public sealed class FeatureFlags
    {
        [JsonProperty]
        public bool IsCleanBeforeBuildEnabled { get; set; }
    }
}
