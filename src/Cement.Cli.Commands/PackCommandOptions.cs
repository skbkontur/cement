using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class PackCommandOptions
{
    public PackCommandOptions(string project, string configuration, BuildSettings buildSettings, bool preRelease)
    {
        Project = project;
        Configuration = configuration;
        BuildSettings = buildSettings;
        PreRelease = preRelease;
    }

    public string Project { get; }
    public string Configuration { get; }
    public BuildSettings BuildSettings { get; }
    public bool PreRelease { get; }
}
