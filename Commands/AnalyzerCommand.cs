using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands;

public sealed class AnalyzerCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly Dictionary<string, ICommand> subCommands;

    public AnalyzerCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, DepsPatcherProject depsPatcherProject,
                           IGitRepositoryFactory gitRepositoryFactory)
    {
        this.consoleWriter = consoleWriter;
        subCommands = new Dictionary<string, ICommand>
        {
            {"add", new AnalyzerAddCommand(consoleWriter, featureFlags, depsPatcherProject, gitRepositoryFactory)}
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
    public bool IsHiddenCommand => false;

    public int Run(string[] args)
    {
        var subCommand = args
            .Skip(1)
            .FirstOrDefault();

        if (subCommand != null && subCommands.ContainsKey(subCommand))
            return subCommands[subCommand].Run(args);

        consoleWriter.WriteError($"Bad arguments: cm analyzer [{subCommand}]");
        consoleWriter.WriteInfo($"Possible arguments: [{string.Join("|", subCommands.Keys)}]");
        return -1;
    }
}
