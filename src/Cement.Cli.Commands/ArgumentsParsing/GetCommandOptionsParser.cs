using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class GetCommandOptionsParser : ArgumentParser<GetCommandOptions>
{
    public override GetCommandOptions Parse(string[] args)
    {
        var reset = false;
        var force = false;
        var pullAnyway = false;
        string configuration = null;
        string merged = null;
        var verbose = false;
        int? gitDepth = null;

        var parser = new OptionSet
        {
            {"r|reset", _ => reset = true},
            {"p|pull-anyway", _ => pullAnyway = true},
            {"c|configuration=", conf => configuration = conf},
            {"f|force", _ => force = true},
            {"m|merged:", m => merged = m ?? "master"},
            {"v|verbose", _ => verbose = true},
            {"git-depth=", d => gitDepth = int.Parse(d)}
        };

        if ((force && reset) || (force && pullAnyway) || (reset && pullAnyway))
        {
            throw new BadArgumentException();
        }

        string moduleName = null;
        string treeish = null;

        var extraArgs = parser.Parse(args.Skip(1));
        if (extraArgs.Count > 0)
        {
            var module = new Dep(extraArgs[0]);
            if (module.Configuration != null)
                configuration = module.Configuration;
            if (module.Treeish != null)
                treeish = module.Treeish;

            moduleName = module.Name;

            if (extraArgs.Count > 1)
            {
                treeish = extraArgs[1];
            }

            ThrowIfHasExtraArgs(extraArgs.Skip(2).ToList());
        }

        if (string.IsNullOrEmpty(moduleName))
            throw new CementException("You should specify the name of the module");

        var policy = PolicyMapper.GetLocalChangesPolicy(force, reset, pullAnyway);
        return new GetCommandOptions(configuration, policy, moduleName, treeish, merged, verbose, gitDepth);
    }
}
