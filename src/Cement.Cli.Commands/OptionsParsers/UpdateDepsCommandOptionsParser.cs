using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class UpdateDepsCommandOptionsParser : OptionsParser<UpdateDepsCommandOptions>
{
    public override UpdateDepsCommandOptions Parse(string[] args)
    {
        var reset = false;
        var force = false;
        var pullAnyway = false;
        string configuration = null;
        string merged = null;
        var localBranchForce = false;
        var verbose = false;
        int? gitDepth = null;

        var parser = new OptionSet
        {
            {"r|reset", _ => reset = true},
            {"p|pull-anyway", _ => pullAnyway = true},
            {"c|configuration=", conf => configuration = conf},
            {"f|force", _ => force = true},
            {"m|merged:", m => merged = m ?? "master"},
            {"allow-local-branch-force", _ => localBranchForce = true},
            {"v|verbose", _ => verbose = true},
            {"git-depth=", d => gitDepth = int.Parse(d)}
        };

        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        if ((force && reset) || (force && pullAnyway) || (reset && pullAnyway))
        {
            throw new BadArgumentException();
        }

        var policy = PolicyMapper.GetLocalChangesPolicy(force, reset, pullAnyway);
        return new UpdateDepsCommandOptions(configuration, merged, policy, localBranchForce, verbose, gitDepth);
    }
}
