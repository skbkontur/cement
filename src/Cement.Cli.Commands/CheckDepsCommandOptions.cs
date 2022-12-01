using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class CheckDepsCommandOptions
{
    public CheckDepsCommandOptions(string configuration, bool showAll, bool findExternal, bool showShort)
    {
        Configuration = configuration;
        ShowAll = showAll;
        FindExternal = findExternal;
        ShowShort = showShort;
    }

    public string Configuration { get; }
    public bool ShowAll { get; }
    public bool FindExternal { get; }
    public bool ShowShort { get; }
}