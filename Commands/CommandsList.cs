using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;

namespace Commands
{
    public sealed class CommandsList : Dictionary<string, ICommand>
    {
        private readonly ConsoleWriter consoleWriter;

        public CommandsList(ConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;

            var readmeGenerator = new ReadmeGenerator(this);
            var cycleDetector = new CycleDetector();
            var usagesProvider = new UsagesProvider(LogManager.GetLogger<UsagesProvider>(), CementSettingsRepository.Get);

            Add("help", new HelpCommand(consoleWriter, readmeGenerator));

            var getCommand = new GetCommand(consoleWriter, cycleDetector);
            Add("get", getCommand);
            Add("update-deps", new UpdateDepsCommand());
            Add("ref", new RefCommand(consoleWriter, getCommand));
            Add("analyzer", new AnalyzerCommand());
            Add("ls", new LsCommand());
            Add("show-configs", new ShowConfigsCommand());
            Add("show-deps", new ShowDepsCommand());
            Add("self-update", new SelfUpdateCommand());
            Add("--version", new VersionCommand());
            Add("build-deps", new BuildDepsCommand());
            Add("build", new BuildCommand());
            Add("check-deps", new CheckDepsCommand());
            Add("check-pre-commit", new CheckPreCommitCommand(consoleWriter));
            Add("usages", new UsagesCommand(consoleWriter, usagesProvider, getCommand));
            Add("init", new InitCommand());
            Add("id", new IdCommand());
            Add("status", new StatusCommand());
            Add("module", new ModuleCommand());
            Add("update", new UpdateCommand());
            Add("convert-spec", new ConvertSpecCommand(consoleWriter));
            Add("reinstall", new ReInstallCommand());
            Add("complete", new CompleteCommand(consoleWriter));
            Add("pack", new PackCommand());
            Add("packages", new PackagesCommand(consoleWriter));
        }

        public void Print()
        {
            var commandNames = Keys.OrderBy(x => x);
            foreach (var commandName in commandNames)
            {
                if (this[commandName].IsHiddenCommand)
                    continue;

                consoleWriter.WriteLine($"{commandName,-25}{GetSmallHelp(commandName)}");
            }
        }

        private string GetSmallHelp(string commandName)
        {
            var help = this[commandName].HelpMessage;
            var lines = help.Split('\n');
            return lines.Length < 2 ? "???" : lines[1].Trim();
        }
    }
}
