using System.Collections.Generic;
using Common;

namespace Commands
{
    public class RefCommand : ICommand
    {
        private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>
        {
            {"add", new RefAdd()},
            {"fix", new RefFix()}
        };

        public int Run(string[] args)
        {
            if (args.Length < 2 || !commands.ContainsKey(args[1]))
            {
                ConsoleWriter.Shared.WriteError("Bad arguments");
                return -1;
            }
            return commands[args[1]].Run(args);
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

        public bool IsHiddenCommand => false;
    }
}
