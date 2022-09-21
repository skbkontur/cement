using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands
{
    public static class CommandsList
    {
        public static readonly Dictionary<string, ICommand> Commands = new()
        {
            {"help", new HelpCommand(ConsoleWriter.Shared, new ReadmeGenerator())},
            {"get", new GetCommand(ConsoleWriter.Shared, new CycleDetector())},
            {"update-deps", new UpdateDepsCommand()},
            {"ref", new RefCommand()},
            {"analyzer", new AnalyzerCommand()},
            {"ls", new LsCommand()},
            {"show-configs", new ShowConfigsCommand()},
            {"show-deps", new ShowDepsCommand()},
            {"self-update", new SelfUpdateCommand()},
            {"--version", new VersionCommand()},
            {"build-deps", new BuildDepsCommand()},
            {"build", new BuildCommand()},
            {"check-deps", new CheckDepsCommand()},
            {"check-pre-commit", new CheckPreCommitCommand(ConsoleWriter.Shared)},
            {"usages", new UsagesCommand()},
            {"init", new InitCommand()},
            {"id", new IdCommand()},
            {"status", new StatusCommand()},
            {"module", new ModuleCommand()},
            {"update", new UpdateCommand()},
            {"convert-spec", new ConvertSpecCommand(ConsoleWriter.Shared)},
            {"reinstall", new ReInstallCommand()},
            {"complete", new CompleteCommand()},
            {"pack", new PackCommand()},
            {"packages", new PackagesCommand(ConsoleWriter.Shared)}
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
