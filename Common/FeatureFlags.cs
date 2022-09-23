using Newtonsoft.Json;

namespace Common
{
    [JsonObject]
    public sealed class FeatureFlags
    {
        public static FeatureFlags Default => new()
        {
            CleanBeforeBuild = false
        };

        [JsonProperty]
        public bool CleanBeforeBuild { get; set; }
    }
}
