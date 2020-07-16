using Newtonsoft.Json;

namespace Common
{
    [JsonObject]
    public sealed class FeatureFlags
    {
        [JsonProperty]
        public bool CleanBeforeBuild { get; set; }

        public static FeatureFlags Default => new FeatureFlags
        {
            CleanBeforeBuild = false
        };
    }
}
