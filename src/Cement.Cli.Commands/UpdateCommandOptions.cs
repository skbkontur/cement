using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UpdateCommandOptions
{
    public UpdateCommandOptions(string treeish, bool verbose, LocalChangesPolicy policy, int? gitDepth)
    {
        Treeish = treeish;
        Verbose = verbose;
        Policy = policy;
        GitDepth = gitDepth;
    }

    public string Treeish { get; }
    public bool Verbose { get; }
    public LocalChangesPolicy Policy { get; }
    public int? GitDepth { get; }
}