using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;

namespace Commands
{
    public class Build : Command
    {
        private string configuration;
        private BuildSettings buildSettings;
        private bool restore;

        public Build()
            : base(new CommandSettings
            {
                LogPerfix = "BUILD",
                LogFileName = "build.net.log",
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseBuildDeps(args);
            configuration = (string) parsedArgs["configuration"];
            buildSettings = new BuildSettings
            {
                ShowAllWarnings = (bool) parsedArgs["warnings"],
                ShowObsoleteWarnings = (bool) parsedArgs["obsolete"],
                ShowOutput = (bool) parsedArgs["verbose"],
                ShowProgress = (bool) parsedArgs["progress"],
                ShowWarningsSummary = true
            };
            restore = (bool) parsedArgs["restore"];
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);
            configuration = configuration ?? "full-build";

            List<Dep> modulesToUpdate;
            List<Dep> topSortedDeps;
            Dictionary<string, string> currentCommitHases;

            if (!new ConfigurationParser(new FileInfo(cwd)).ConfigurationExists(configuration))
            {
                ConsoleWriter.WriteError($"Configuration '{configuration}' was not found in {moduleName}.");
                return -1;
            }

            var builder = new ModuleBuilder(Log, buildSettings);
            var builderInitTask = Task.Run(() => builder.Init());

            new BuildPreparer(Log).GetModulesOrder(moduleName, configuration, out topSortedDeps, out modulesToUpdate, out currentCommitHases);

            var builtStorage = BuiltInfoStorage.Deserialize();
            builtStorage.RemoveBuildInfo(moduleName);

            builderInitTask.Wait();
            var module = new Dep(moduleName, null, configuration);
            
            BuildDeps.TryNugetRestore(new List<Dep> {module}, builder);

            if (!builder.Build(module))
            {
                builtStorage.Save();
                return -1;
            }
            builtStorage.AddBuiltModule(module, currentCommitHases);
            builtStorage.Save();
            return 0;
        }

        public override string HelpMessage => @"
    Performs build for the current module

    Usage:
        cm build [-v|--verbose|-w|-W|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -c/--configuration      - build corresponding configuration

        -v/--verbose            - show full msbuild output
        -w/--warnings           - show warnings
        -W                      - show only obsolete warnings

        -p/--progress           - show msbuild output in one line
";
    }
}