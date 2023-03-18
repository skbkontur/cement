using System.Linq;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class ChangeModuleCommandOptionsParser : OptionsParser<ChangeModuleCommandOptions>
{
    public override ChangeModuleCommandOptions Parse(string[] args)
    {
        string pushUrl = null;
        string packageName = null;

        var parser = new OptionSet
        {
            {"p|pushurl=", p => pushUrl = p},
            {"package=", p => packageName = p}
        };

        var extraArgs = parser.Parse(args.Skip(1));
        if (extraArgs.Count < 3)
        {
            throw new BadArgumentException("Too few arguments. \n" +
                                           "Using: cm module <add|change> module_name module_fetch_url " +
                                           "[-p|--pushurl=module_push_url] [--package=package_name]");
        }

        var moduleName = extraArgs[1];
        var fetchUrl = extraArgs[2];

        extraArgs = extraArgs.Skip(3).ToList();
        ThrowIfHasExtraArgs(extraArgs);

        return new ChangeModuleCommandOptions(moduleName, pushUrl, fetchUrl, packageName);
    }
}
