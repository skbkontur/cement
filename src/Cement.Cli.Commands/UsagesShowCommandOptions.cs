using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UsagesShowCommandOptions
{
    public UsagesShowCommandOptions(string module, string branch, string configuration, bool showAll, bool printEdges)
    {
        Module = module;
        Branch = branch;
        Configuration = configuration;
        ShowAll = showAll;
        PrintEdges = printEdges;
    }

    public string Module { get; }
    public string Branch { get; }
    public string Configuration { get; }
    public bool ShowAll { get; }
    public bool PrintEdges { get; }
}