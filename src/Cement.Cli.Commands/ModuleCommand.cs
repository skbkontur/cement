using System;
using System.Collections.Generic;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class ModuleCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly ICommandActivator commandActivator;
    private readonly Dictionary<string, Type> commands;

    public ModuleCommand(ConsoleWriter consoleWriter, ICommandActivator commandActivator)
    {
        this.consoleWriter = consoleWriter;
        this.commandActivator = commandActivator;
        commands = new Dictionary<string, Type>
        {
            {"add", typeof(AddModuleCommand)},
            {"change", typeof(ChangeModuleCommand)}
        };
    }

    public bool MeasureElapsedTime { get; }

    public bool RequireModuleYaml { get; }

    public CommandLocation Location { get; } = CommandLocation.Any;

    public string Name => "module";

    public string HelpMessage => @"
    Adds new or changes existing cement module
    Don't delete old modules

    Usage:
        cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]
        --pushurl        - module push url
        --package        - name of repository with modules description, specify if multiple
";

    public int Run(string[] args)
    {
        if (args.Length < 2 || !commands.ContainsKey(args[1]))
        {
            consoleWriter.WriteError("Bad arguments");
            return -1;
        }

        var commandType = commands[args[1]];
        var command = (ICommand)commandActivator.Create(commandType);

        return command.Run(args);
    }
}
