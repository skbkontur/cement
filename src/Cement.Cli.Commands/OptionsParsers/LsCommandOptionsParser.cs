using System.Linq;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class LsCommandOptionsParser : OptionsParser<LsCommandOptions>
{
    public override LsCommandOptions Parse(string[] args)
    {
        var isLocal = false;
        var isAllModules = false;
        string branchName = null;
        var isSimpleMode = false;
        var showUrl = false;
        var showPushUrl = false;

        var parser = new OptionSet
        {
            {"l|local", _ => isLocal = true},
            {"a|all", _ => isAllModules = true},
            {"b|has-branch=", branch => branchName = branch},
            {"u|url", _ => showUrl = true},
            {"p|pushurl", _ => showPushUrl = true},
            {"simple", _ => isSimpleMode = true}
        };

        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        var moduleProcessType = GetModuleProcessType(isLocal, isAllModules, branchName);
        return new LsCommandOptions(isSimpleMode, moduleProcessType, showUrl, showPushUrl, branchName);
    }

    private static ModuleProcessType GetModuleProcessType(bool isLocal, bool isAllModules, string branchName)
    {
        if (isLocal && isAllModules)
            throw new BadArgumentException("Bad arguments: all and local");

        if (isLocal)
            return ModuleProcessType.Local;

        if (isAllModules)
            return ModuleProcessType.All;

        return branchName is null
            ? ModuleProcessType.All
            : ModuleProcessType.Local;
    }
}
