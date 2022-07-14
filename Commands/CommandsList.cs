using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands
{
    public static class CommandsList
    {
        public static readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>
        {
            {"help", new Help(ConsoleWriter.Shared, new ReadmeGenerator())},
            {"get", new Get()},
            {"update-deps", new UpdateDeps()},
            {"ref", new RefCommand()},
            {"analyzer", new AnalyzerCommand()},
            {"ls", new Ls()},
            {"show-configs", new ShowConfigs()},
            {"show-deps", new ShowDeps()},
            {"self-update", new SelfUpdate()},
            {"--version", new CementVersion()},
            {"build-deps", new BuildDeps()},
            {"build", new Build()},
            {"check-deps", new CheckDeps()},
            {"check-pre-commit", new CheckPreCommit()},
            {"usages", new UsagesCommand()},
            {"init", new Init()},
            {"id", new IdCommand()},
            {"status", new Status()},
            {"module", new ModuleCommand()},
            {"update", new Update()},
            {"convert-spec", new ConvertSpec()},
            {"reinstall", new ReInstall()},
            {"complete", new CompleteCommand()},
            {"pack", new PackCommand()}
        };

        public static void Print()
        {
            var commandNames = Commands.Keys.OrderBy(x => x);
            foreach (var commandName in commandNames)
            {
                if (Commands[commandName].IsHiddenCommand)
                    continue;
                ConsoleWriter.Shared.WriteLine($"{commandName,-25}{GetSmallHelp(commandName)}");
            }
        }

        private static string GetSmallHelp(string commandName)
        {
            var help = Commands[commandName].HelpMessage;
            var lines = help.Split('\n');
            return lines.Length < 2 ? "???" : lines[1].Trim();
        }
    }
}
