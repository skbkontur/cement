using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands;

public sealed class PackagesCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDictionary<string, ICommand> subcommands;

    public PackagesCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
    {
        this.consoleWriter = consoleWriter;
        subcommands = new Dictionary<string, ICommand>
        {
            {"list", new ListPackagesCommand(consoleWriter, featureFlags)},
            {"add", new AddPackageCommand(consoleWriter, featureFlags)},
            {"remove", new RemovePackageCommand(consoleWriter, featureFlags)}
        };
    }

    public string Name => "packages";
    public bool IsHiddenCommand => true;
    public string HelpMessage => @"
usage: cm packages list
   or: cm packages add <name> <url>
   or: cm packages remove <name>
";

    public int Run(string[] args)
    {
        if (args.Length >= 2)
        {
            if (subcommands.ContainsKey(args[1]))
                return subcommands[args[1]].Run(args.Skip(2).ToArray());

            consoleWriter.WriteError($"Unknown subcommand: {args[1]}{Environment.NewLine}{HelpMessage}");
        }
        else
        {
            consoleWriter.WriteError($"Unknown subcommand{Environment.NewLine}{HelpMessage}");
        }

        return -1;
    }
}
