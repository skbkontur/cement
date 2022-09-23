using System.IO;
using Common;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class UpdateCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "update",
            Location = CommandSettings.CommandLocation.RootModuleDirectory
        };

        private readonly ConsoleWriter consoleWriter;
        private string treeish = "master";
        private bool verbose;
        private LocalChangesPolicy policy;
        private int? gitDepth;

        public UpdateCommand(ConsoleWriter consoleWriter)
            : base(consoleWriter, Settings)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string HelpMessage => @"
    Updates module for current directory

    Usage:
        cm update [-f/-r/-p] [-v] [treeish]

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -v/--verbose                show commit info for deps

        --git-depth <depth>         adds '--depth <depth>' flag to git commands

    This command runs 'update' ('git pull origin treeish') command for module
    If treeish isn't specified, cement uses current
";

        protected override int Execute()
        {
            Log.LogInformation("Updating packages");
            PackageUpdater.Shared.UpdatePackages();
            var cwd = Directory.GetCurrentDirectory();
            var module = Path.GetFileName(cwd);

            var curRepo = new GitRepository(module, Helper.CurrentWorkspace, Log);
            if (treeish == null)
                treeish = curRepo.CurrentLocalTreeish().Value;

            var getter = new ModuleGetter(
                consoleWriter,
                Helper.GetModules(),
                new Dep(module, treeish),
                policy,
                null,
                verbose,
                gitDepth: gitDepth);

            getter.GetModule();

            return 0;
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseUpdate(args);
            treeish = (string)parsedArgs["treeish"];
            verbose = (bool)parsedArgs["verbose"];
            policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
            gitDepth = (int?)parsedArgs["gitDepth"];
        }
    }
}
