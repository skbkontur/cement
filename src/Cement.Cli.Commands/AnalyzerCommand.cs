using System;
using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class AnalyzerCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly ICommandActivator commandActivator;
    private readonly Dictionary<string, Type> subCommands;

    public AnalyzerCommand(ConsoleWriter consoleWriter, ICommandActivator commandActivator)
    {
        this.consoleWriter = consoleWriter;
        this.commandActivator = commandActivator;
        subCommands = new Dictionary<string, Type>
        {
            {"add", typeof(AnalyzerAddCommand)}
        };
    }

    public string HelpMessage => @"
    Adds analyzers in *.sln

    analyzer add
        Adds analyzer target reference assemblies to msbuild project files into solution

        Usage:
            cm analyzer add <module-name>/[<configuration>] [<solution-file>]

        Example:
            cm analyzer add analyzers.async-code/warn
                Adds analyzer from module analyzers.code-style to all projects
                in current solution and adds analyzers.code-style to 'module.yaml' file
            cm analyzer add analyzers.async-code mysolution.sln
                Adds analyzer from module analyzers.code-style to all projects
                in mysolution.sln and adds analyzers.code-style to 'module.yaml' file
";

    public string Name => "analyzer";

    public int Run(string[] args)
    {
        var subCommand = args
            .Skip(1)
            .FirstOrDefault();

        if (subCommand != null && subCommands.ContainsKey(subCommand))
        {
            var commandType = subCommands[subCommand];
            var command = (ICommand)commandActivator.Create(commandType);

            return command.Run(args);
        }

        consoleWriter.WriteError($"Bad arguments: cm analyzer [{subCommand}]");
        consoleWriter.WriteInfo($"Possible arguments: [{string.Join("|", subCommands.Keys)}]");
        return -1;
    }
}
