using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class UsagesGrepCommandOptionsParser : OptionsParser<UsagesGrepCommandOptions>
{
    public override UsagesGrepCommandOptions Parse(string[] args)
    {
        var gitArgs = new List<string>();
        string branch = null;
        var skipGet = false;

        var parser = new OptionSet
        {
            {"b|branch=", b => branch = b},
            {"s|skip-get", _ => skipGet = true},
            {"<>", b => gitArgs.Add(b)}
        };

        var delimPosition = Array.IndexOf(args, "--");
        if (delimPosition < 0)
            delimPosition = args.Length;

        parser.Parse(args.Take(delimPosition));

        var fileMasks = args.Skip(delimPosition + 1).ToArray();
        return new UsagesGrepCommandOptions(gitArgs.ToArray(), fileMasks, skipGet, branch);
    }
}
