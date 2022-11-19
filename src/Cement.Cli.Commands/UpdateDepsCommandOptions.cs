using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UpdateDepsCommandOptions
{
    public UpdateDepsCommandOptions(string configuration, string mergedBranch, LocalChangesPolicy policy, bool localBranchForce,
                                    bool verbose, int? gitDepth)
    {
        Configuration = configuration;
        MergedBranch = mergedBranch;
        Policy = policy;
        LocalBranchForce = localBranchForce;
        Verbose = verbose;
        GitDepth = gitDepth;
    }

    public string Configuration { get; }
    public string MergedBranch { get; }
    public LocalChangesPolicy Policy { get; }
    public bool LocalBranchForce { get; }
    public bool Verbose { get; }
    public int? GitDepth { get; }
}