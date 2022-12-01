using Newtonsoft.Json;

namespace Cement.Cli.Common;

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
