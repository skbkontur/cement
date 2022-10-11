using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class BuildDepsCommandOptions
{
    public BuildDepsCommandOptions(string configuration, bool rebuild, bool parallel, BuildSettings buildSettings)
    {
        Configuration = configuration;
        Rebuild = rebuild;
        Parallel = parallel;
        BuildSettings = buildSettings;
    }

    public string Configuration { get; }

    public bool Rebuild { get; }

    public bool Parallel { get; }

    public BuildSettings BuildSettings { get; }
}
