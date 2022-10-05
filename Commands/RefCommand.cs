using System.Collections.Generic;
using Common;

namespace Commands;

public sealed class RefCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly Dictionary<string, ICommand> commands;

    public RefCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, GetCommand getCommand,
                      BuildDepsCommand buildDepsCommand, BuildCommand buildCommand, DepsPatcherProject depsPatcherProject)
    {
        this.consoleWriter = consoleWriter;
        commands = new Dictionary<string, ICommand>
        {
            {"add", new RefAddCommand(consoleWriter, featureFlags, getCommand, buildDepsCommand, buildCommand, depsPatcherProject)},
            {"fix", new RefFixCommand(consoleWriter, featureFlags, depsPatcherProject)}
        };
    }

    public string HelpMessage => @"
    Adds or fixes references in *.csproj

    ref add
        Adds module target reference assemblies to msbuild project file

        Usage:
            cm ref add <module-name>[/configuration] <project-file>

        Example:
            cm ref add nunit myproj.csproj
                Adds reference to nunit.framework.dll to myproj.csproj and adds nunit to 'module.yaml' file

    ref fix
        Fixes deps and references in all csproj files to correct install files

        Usage:
            cm ref fix [-e]
            -e/--external       try to fix references to not cement modules or to current module

        Example:
            change	<HintPath>..\..\props\libprops\bin\Release\4.0\Kontur.Core.dll</HintPath>
            to		<HintPath>..\..\core\bin\Release\Kontur.Core.dll</HintPath>
";

    public string Name => "ref";
    public bool IsHiddenCommand => false;

    public int Run(string[] args)
    {
        if (args.Length < 2 || !commands.ContainsKey(args[1]))
        {
            consoleWriter.WriteError("Bad arguments");
            return -1;
        }

        return commands[args[1]].Run(args);
    }
}
