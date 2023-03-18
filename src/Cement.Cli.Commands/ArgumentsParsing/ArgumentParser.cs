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
