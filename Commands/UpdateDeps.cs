using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public class UpdateDeps : Command
    {
        private string configuration;
        private string mergedBranch;
        private LocalChangesPolicy policy;
        private bool localBranchForce;
        private bool verbose;

        public UpdateDeps()
            : base(new CommandSettings
            {
                LogPerfix = "UPDATE",
                LogFileName = "update-deps.net.log",
                MeasureElapsedTime = true,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            Helper.RemoveOldKey(ref args, "-n", Log);

            var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
            configuration = (string) parsedArgs["configuration"];
            mergedBranch = (string) parsedArgs["merged"];
            localBranchForce = (bool) parsedArgs["localBranchForce"];
            verbose = (bool) parsedArgs["verbose"];
            policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;

            Log.Info("Updating packages");
            PackageUpdater.UpdatePackages();
            var modules = Helper.GetModules();

            var moduleName = Path.GetFileName(cwd);

            var curRepo = new GitRepository(moduleName, Helper.CurrentWorkspace, Log);
            if (curRepo.IsGitRepo)
                curRepo.TryUpdateUrl(modules.FirstOrDefault(m => m.Name.Equals(moduleName)));
            HooksHelper.InstallHooks(moduleName);

            var getter = new ModuleGetter(
                Helper.GetModules(),
                new Dep(moduleName, null, configuration),
                policy,
                mergedBranch,
                verbose,
                localBranchForce);

            getter.GetDeps();


            Log.Info("SUCCESS UPDATE DEPS");
            return 0;
        }

        public override string HelpMessage => @"
    Updates deps for current directory

    Usage:
        cm update-deps [-f/-p/-r] [--bin] [-m] [-c <config-name>] [--allow-local-branch-force] [-v]

        -c/--configuration          updates deps for corresponding configuration

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state. Checks for 'master' by default

        --allow-local-branch-force  allows forcing local-only branches

        -v/--verbose                show commit info for deps

    Example:
        cm update-deps -r --progress
";
    }
}