using System;
using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class PackagesCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly ICommandActivator commandActivator;
    private readonly IDictionary<string, Type> subcommands;

    public PackagesCommand(ConsoleWriter consoleWriter, ICommandActivator commandActivator)
    {
        this.consoleWriter = consoleWriter;
        this.commandActivator = commandActivator;
        subcommands = new Dictionary<string, Type>
        {
            {"list", typeof(ListPackagesCommand)},
            {"add", typeof(AddPackageCommand)},
            {"remove", typeof(RemovePackageCommand)}
        };
    }

    public string Name => "packages";

    public string HelpMessage => @"
    Manage set of packages

    usage: cm packages list
       or: cm packages add <name> <url>
       or: cm packages remove <name>
";

    public int Run(string[] args)
    {
        if (args.Length >= 2)
        {
            if (subcommands.ContainsKey(args[1]))
            {
                var commandType = subcommands[args[1]];
                var command = (ICommand)commandActivator.Create(commandType);

                return command.Run(args.Skip(2).ToArray());
            }

            consoleWriter.WriteError($"Unknown subcommand: {args[1]}{Environment.NewLine}{HelpMessage}");
        }
        else
        {
            consoleWriter.WriteError($"Unknown subcommand{Environment.NewLine}{HelpMessage}");
        }

        return -1;
    }
}
