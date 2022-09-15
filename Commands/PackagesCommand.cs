using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands;

public sealed class PackagesCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDictionary<string, ICommand> subcommands;

    public PackagesCommand(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;

        //dstarasov: осознано не используем consoleWriter из аргументов конструктора, т.к. PackagesCommand вообще ничего
        //dstarasov: не должен знать про то, как создавать его подкомманды и в будущем эта ответственность отсюда уедет
        subcommands = new Dictionary<string, ICommand>
        {
            {"list", new ListPackagesCommand(ConsoleWriter.Shared)},
            {"add", new AddPackageCommand()},
            {"remove", new RemovePackageCommand()}
        };
    }

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
