using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public static class ArgumentParser
{
    public static Dictionary<string, object> ParseBuildParents(string[] args)
    {
        var parsedArguments = new Dictionary<string, object>
        {
            {"branch", null},
            {"pause", false}
        };
        var parser = new OptionSet
        {
            {"b|branch=", b => parsedArguments["branch"] = b},
            {"p|pause", b => parsedArguments["pause"] = true}
        };
        var extraArgs = parser.Parse(args.Skip(2));
        ThrowIfHasExtraArgs(extraArgs);
        return parsedArguments;
    }

    public static Dictionary<string, object> ParseGrepParents(string[] args)
    {
        var gitArgs = new List<string>();

        var parsedArguments = new Dictionary<string, object>
        {
            {"branch", null},
            {"skip-get", false}
        };
        var parser = new OptionSet
        {
            {"b|branch=", b => parsedArguments["branch"] = b},
            {"s|skip-get", b => parsedArguments["skip-get"] = true},
            {"<>", b => gitArgs.Add(b)}
        };

        var delimPosition = Array.IndexOf(args, "--");
        if (delimPosition < 0)
            delimPosition = args.Length;

        parser.Parse(args.Take(delimPosition));
        parsedArguments["gitArgs"] = gitArgs.ToArray();
        parsedArguments["fileMaskArgs"] = args.Skip(delimPosition + 1).TakeWhile(_ => true).ToArray();
        return parsedArguments;
    }

    public static Dictionary<string, object> ParseModuleCommand(string[] args)
    {
        var parsedArguments = new Dictionary<string, object>
        {
            {"pushurl", null},
            {"package", null}
        };
        var parser = new OptionSet
        {
            {"p|pushurl=", p => parsedArguments["pushurl"] = p},
            {"package=", p => parsedArguments["package"] = p}
        };

        var extraArgs = parser.Parse(args.Skip(1));
        if (extraArgs.Count < 3)
            throw new BadArgumentException("Too few arguments. \nUsing: cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]");

        parsedArguments["command"] = extraArgs[0];
        parsedArguments["module"] = extraArgs[1];
        parsedArguments["fetchurl"] = extraArgs[2];
        extraArgs = extraArgs.Skip(3).ToList();

        ThrowIfHasExtraArgs(extraArgs);
        return parsedArguments;
    }

    public static Dictionary<string, object> ParseShowConfigs(string[] args)
    {
        var parsedArgs = new Dictionary<string, object>
        {
            {"module", null}
        };
        var extraArgs = args.Skip(1).ToList();
        if (extraArgs.Count > 0)
        {
            parsedArgs["module"] = extraArgs[0];
            ThrowIfHasExtraArgs(extraArgs.Skip(1).ToList());
        }

        return parsedArgs;
    }

    private static void ThrowIfHasExtraArgs(List<string> extraArgs)
    {
        if (extraArgs.Count > 0)
            throw new BadArgumentException("Extra arguments: " + string.Join(", ", extraArgs));
    }
}
