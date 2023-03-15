using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class UpdateCommandOptionsParser : ArgumentParser<UpdateCommandOptions>
{
    public override UpdateCommandOptions Parse(string[] args)
    {
        string treeish = null;
        var reset = false;
        var force = false;
        var pullAnyway = false;
        var verbose = false;
        int? gitDepth = null;

        var parser = new OptionSet
        {
            {"r|reset", _ => reset = true},
            {"p|pull-anyway", _ => pullAnyway = true},
            {"f|force", _ => force = true},
            {"v|verbose", _ => verbose = true},
            {"git-depth=", d => gitDepth = int.Parse(d)}
        };
        var extraArgs = parser.Parse(args.Skip(1));
        if (extraArgs.Count > 0)
        {
            treeish = extraArgs[0];
            ThrowIfHasExtraArgs(extraArgs.Skip(1).ToList());
        }

        if ((force && reset) || (force && pullAnyway) || (reset && pullAnyway))
        {
            throw new BadArgumentException();
        }

        var policy = PolicyMapper.GetLocalChangesPolicy(force, reset, pullAnyway);
        return new UpdateCommandOptions(treeish, verbose, policy, gitDepth);
    }
}
