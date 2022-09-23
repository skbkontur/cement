using System.Collections.Generic;
using Common;
using Common.Logging;

namespace Commands
{
    public sealed class CommandsList : Dictionary<string, ICommand>
    {
        public CommandsList(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        {
            var readmeGenerator = new ReadmeGenerator(this);
            var cycleDetector = new CycleDetector();
            var usagesProvider = new UsagesProvider(LogManager.GetLogger<UsagesProvider>(), CementSettingsRepository.Get);

            Add("help", new HelpCommand(consoleWriter, featureFlags, readmeGenerator));

            var getCommand = new GetCommand(consoleWriter, featureFlags, cycleDetector);
            Add("get", getCommand);
            Add("update-deps", new UpdateDepsCommand(consoleWriter, featureFlags));
            Add("ref", new RefCommand(consoleWriter, featureFlags, getCommand));
            Add("analyzer", new AnalyzerCommand(consoleWriter, featureFlags));
            Add("ls", new LsCommand(consoleWriter));
            Add("show-configs", new ShowConfigsCommand(consoleWriter, featureFlags));
            Add("show-deps", new ShowDepsCommand(consoleWriter, featureFlags));
            Add("self-update", new SelfUpdateCommand(consoleWriter, featureFlags));
            Add("--version", new VersionCommand(consoleWriter));
            Add("build-deps", new BuildDepsCommand(consoleWriter, featureFlags));
            Add("build", new BuildCommand(consoleWriter, featureFlags));
            Add("check-deps", new CheckDepsCommand(consoleWriter, featureFlags));
            Add("check-pre-commit", new CheckPreCommitCommand(consoleWriter, featureFlags));
            Add("usages", new UsagesCommand(consoleWriter, featureFlags, usagesProvider, getCommand));
            Add("init", new InitCommand(consoleWriter));
            Add("id", new IdCommand(consoleWriter));
            Add("status", new StatusCommand(consoleWriter));

            var moduleHelper = new ModuleHelper(LogManager.GetLogger<ModuleHelper>(), consoleWriter);
            Add("module", new ModuleCommand(consoleWriter, moduleHelper));
            Add("update", new UpdateCommand(consoleWriter, featureFlags));
            Add("convert-spec", new ConvertSpecCommand(consoleWriter, featureFlags));
            Add("reinstall", new ReInstallCommand(consoleWriter, featureFlags));
            Add("complete", new CompleteCommand(consoleWriter, featureFlags));
            Add("pack", new PackCommand(consoleWriter, featureFlags));
            Add("packages", new PackagesCommand(consoleWriter, featureFlags));
        }
    }
}
