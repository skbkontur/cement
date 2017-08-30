using System.IO;
using Common;

namespace Commands
{
    public class Get : Command
    {
        private string configuration;
        private LocalChangesPolicy policy;
        private string module;
        private string treeish;
        private string mergedBranch;
        private bool verbose;

        public Get()
            : base(new CommandSettings
            {
                LogPerfix = "GET",
                LogFileName = "get.net.log",
                MeasureElapsedTime = true,
                Location = CommandSettings.CommandLocation.WorkspaceDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            Helper.RemoveOldKey(ref args, "-n", Log);

            var parsedArgs = ArgumentParser.ParseGet(args);
            module = (string) parsedArgs["module"];
            if (string.IsNullOrEmpty(module))
                throw new CementException("You should specify the name of the module");

            treeish = (string) parsedArgs["treeish"];
            configuration = (string) parsedArgs["configuration"];
            mergedBranch = (string) parsedArgs["merged"];
            verbose = (bool) parsedArgs["verbose"];
            policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
        }

        protected override int Execute()
        {
            string workspace = Directory.GetCurrentDirectory();
            if (!Helper.IsCurrentDirectoryModule(Path.Combine(workspace, module)))
                throw new CementTrackException($"{workspace} is not cement workspace directory.");

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;

            Log.Info("Updating packages");
            PackageUpdater.UpdatePackages();

            GetModule();
            CycleDetector.WarnIfCycle(module, configuration, Log);

            Log.Info("SUCCESS get " + module);
            return 0;
        }

        private void GetModule()
        {
            var getter = new ModuleGetter(
                Helper.GetModules(),
                new Dep(module, treeish, configuration),
                policy,
                mergedBranch,
                verbose);

            getter.GetModule();

            ConsoleWriter.WriteInfo("Getting deps for " + module);
            Log.Info("Getting deps list for " + module);

            getter.GetDeps();
        }

        public override string HelpMessage => @"
    Gets module with all deps

    Usage:
        cm get [-f/-r/-p] [-v] [-m[=branch]] [-c <config-name>] module_name[/configuration][@treeish] [treeish]
        cm get module_name@treeish/configuration

        -c/--configuration          gets deps for corresponding configuration

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state. 
                                    Checks for 'master' by default

        -v/--verbose                show commit info for deps

    Example:
        cm get kanso/client@release -rv
        cm get kanso -c client release -rv
";
    }
}