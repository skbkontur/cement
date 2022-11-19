using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cement.Cli.Common;

[JsonObject]
public sealed class BuildInfoStorageData
{
    public BuildInfoStorageData()
    {
        ModulesWithDeps = new Dictionary<Dep, List<DepWithCommitHash>>();
    }

    [JsonProperty("ModulesWithDeps")]
    public Dictionary<Dep, List<DepWithCommitHash>> ModulesWithDeps { get; }
}
