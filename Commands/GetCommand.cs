using System.IO;
using Common;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class GetCommand : Command
    {
        private readonly ConsoleWriter consoleWriter;
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "get",
            MeasureElapsedTime = true,
            Location = CommandSettings.CommandLocation.WorkspaceDirectory
        };

        private readonly CycleDetector cycleDetector;
        private string configuration;
        private LocalChangesPolicy policy;
        private string module;
        private string treeish;
        private string mergedBranch;
        private bool verbose;
        private int? gitDepth;

        public GetCommand(ConsoleWriter consoleWriter, CycleDetector cycleDetector)
            : base(Settings)
        {
            this.consoleWriter = consoleWriter;
            this.cycleDetector = cycleDetector;
        }

        public override string HelpMessage => @"
    Gets module with all deps

    Usage:
        cm get [-f/-r/-p] [-v] [-m[=branch]] [-c <config-name>] module_name[/configuration][@treeish] [treeish]
        cm get module_name@treeish/configuration

        -c/--configuration          gets deps for corresponding configuration

        -f/--force                  forces local changes(not pulling from remote)
        -r/--reset                  resets all local changes
        -p/--pull-anyway            tries to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state.
                                    checks for 'master' by default

        -v/--verbose                show commit info for deps

        --git-depth <depth>         adds '--depth <depth>' flag to git commands

    Example:
        cm get kanso/client@release -rv
        cm get kanso -c client release -rv
";

        protected override void ParseArgs(string[] args)
        {
            Helper.RemoveOldKey(ref args, "-n", Log);

            var parsedArgs = ArgumentParser.ParseGet(args);
            module = (string)parsedArgs["module"];
            if (string.IsNullOrEmpty(module))
                throw new CementException("You should specify the name of the module");

            treeish = (string)parsedArgs["treeish"];
            configuration = (string)parsedArgs["configuration"];
            mergedBranch = (string)parsedArgs["merged"];
            verbose = (bool)parsedArgs["verbose"];
            gitDepth = (int?)parsedArgs["gitDepth"];
            policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
        }

        protected override int Execute()
        {
            var workspace = Directory.GetCurrentDirectory();
            if (!Helper.IsCurrentDirectoryModule(Path.Combine(workspace, module)))
                throw new CementTrackException($"{workspace} is not cement workspace directory.");

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;

            Log.LogInformation("Updating packages");
            PackageUpdater.UpdatePackages();

            GetModule();
            cycleDetector.WarnIfCycle(module, configuration, Log);

            Log.LogInformation("SUCCESS get " + module);
            return 0;
        }

        private void GetModule()
        {
            var getter = new ModuleGetter(
                Helper.GetModules(),
                new Dep(module, treeish, configuration),
                policy,
                mergedBranch,
                verbose,
                gitDepth: gitDepth);

            getter.GetModule();

            consoleWriter.WriteInfo("Getting deps for " + module);
            Log.LogInformation("Getting deps list for " + module);

            getter.GetDeps();
        }
    }
}
