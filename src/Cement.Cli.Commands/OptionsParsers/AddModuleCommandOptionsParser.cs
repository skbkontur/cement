﻿using System.Linq;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class AddModuleCommandOptionsParser : OptionsParser<AddModuleCommandOptions>
{
    public override AddModuleCommandOptions Parse(string[] args)
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
            throw new BadArgumentException(
                "Too few arguments. \n" +
                "Using: cm module <add|change> module_name module_fetch_url " +
                "[-p|--pushurl=module_push_url] [--package=package_name]");
        }

        var moduleName = extraArgs[1];
        var fetchUrl = extraArgs[2];

        extraArgs = extraArgs.Skip(3).ToList();
        ThrowIfHasExtraArgs(extraArgs);

        return new AddModuleCommandOptions(moduleName, pushUrl, fetchUrl, packageName);
    }
}
