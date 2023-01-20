using System;
using System.Collections.Generic;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RefCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly ICommandActivator commandActivator;
    private readonly Dictionary<string, Type> commands;

    public RefCommand(ConsoleWriter consoleWriter, ICommandActivator commandActivator)
    {
        this.consoleWriter = consoleWriter;
        this.commandActivator = commandActivator;
        commands = new Dictionary<string, Type>
        {
            {"add", typeof(RefAddCommand)},
            {"fix", typeof(RefFixCommand)}
        };
    }

    public bool MeasureElapsedTime { get; }

    public bool RequireModuleYaml { get; }

    public CommandLocation Location { get; } = CommandLocation.Any;

    public string Name => "ref";

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
