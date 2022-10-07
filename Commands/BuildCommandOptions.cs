using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class BuildCommandOptions
{
    public BuildCommandOptions(string configuration, BuildSettings buildSettings)
    {
        Configuration = configuration;
        BuildSettings = buildSettings;
    }

    public string Configuration { get; }
    public BuildSettings BuildSettings { get; }
}