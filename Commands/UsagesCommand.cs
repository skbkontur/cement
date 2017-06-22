using System.Collections.Generic;
using Common;

namespace Commands
{
    public class UsagesCommand : ICommand
    {
        private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>
        {
            {"show", new UsagesShow()},
            {"build", new UsagesBuild()}
        };

        public int Run(string[] args)
        {
            if (args.Length < 2 || !commands.ContainsKey(args[1]))
            {
                ConsoleWriter.WriteError("Bad arguments");
                return -1;
            }
            return commands[args[1]].Run(args);
        }

        public string HelpMessage => @"
    Performs operations with module usages

    usages show
        shows the modules linked to the given dependence

        Usage:
            cm usages show [-m=<module>] [-c=<configuration>] [-b=<branch>] [-a]
            -m/--module            - module name (current module name by default)
            -c/--configuration     - configuration name (* by default)
            -b/--branch            - branch name (* by default)
            -a/--all               - show every branch of each parent
            -e/--edges             - prints graph in proper format for graph visualizers(i.e. arborjs.org/halfviz/)

        Example:
            cm usages show -m=logging
                show the modules which linked to the logging/full-build master

    usages build
        tries get and build all modules (in masters) linked to the current

        Usage:
            cm usages build [-b=<branch>] [-p]
            -b/--branch            - checking parents which use this branch (current by default)
            -p/--pause             - pause on errors
";

        public bool IsHiddenCommand => CementSettings.Get().CementServer == null;
    }
}
