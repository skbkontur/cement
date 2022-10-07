using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class GetCommandOptions
{
    public GetCommandOptions(string configuration, LocalChangesPolicy policy, string module, string treeish, string mergedBranch,
                             bool verbose, int? gitDepth)
    {
        Configuration = configuration;
        Policy = policy;
        Module = module;
        Treeish = treeish;
        MergedBranch = mergedBranch;
        Verbose = verbose;
        GitDepth = gitDepth;
    }

    public string Configuration { get; }
    public LocalChangesPolicy Policy { get; }
    public string Module { get; }
    public string Treeish { get; }
    public string MergedBranch { get; }
    public bool Verbose { get; }
    public int? GitDepth { get; }
}