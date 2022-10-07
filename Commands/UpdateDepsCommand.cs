using System.IO;
using System.Linq;
using Common;
using Common.DepsValidators;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Commands;

[PublicAPI]
public sealed class UpdateDepsCommand : Command<UpdateDepsCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "update-deps",
        MeasureElapsedTime = true,
        Location = CommandLocation.RootModuleDirectory
    };

    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public UpdateDepsCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, CycleDetector cycleDetector,
                             IDepsValidatorFactory depsValidatorFactory, IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.cycleDetector = cycleDetector;
        this.depsValidatorFactory = depsValidatorFactory;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public override string Name => "update-deps";
    public override string HelpMessage => @"
    Updates deps for current directory

    Usage:
        cm update-deps [-f/-p/-r] [--bin] [-m] [-c <config-name>] [--allow-local-branch-force] [-v]

        -c/--configuration          updates deps for corresponding configuration

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state.
                                    Checks for 'master' by default

        --allow-local-branch-force  allows forcing local-only branches

        -v/--verbose                show commit info for deps

        --git-depth <depth>         adds '--depth <depth>' flag to git commands

    Example:
        cm update-deps -r --progress
";

    protected override UpdateDepsCommandOptions ParseArgs(string[] args)
    {
        Helper.RemoveOldKey(ref args, "-n", Log);

        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        var configuration = (string)parsedArgs["configuration"];
        var mergedBranch = (string)parsedArgs["merged"];
        var localBranchForce = (bool)parsedArgs["localBranchForce"];
        var verbose = (bool)parsedArgs["verbose"];
        var policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
        var gitDepth = (int?)parsedArgs["gitDepth"];

        return new UpdateDepsCommandOptions(configuration, mergedBranch, policy, localBranchForce, verbose, gitDepth);
    }

    protected override int Execute(UpdateDepsCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();

        var configuration = string.IsNullOrEmpty(options.Configuration) ? "full-build" : options.Configuration;

        Log.LogInformation("Updating packages");
        PackageUpdater.Shared.UpdatePackages();
        var modules = Helper.GetModules();

        var moduleName = Path.GetFileName(cwd);

        var curRepo = gitRepositoryFactory.Create(moduleName, Helper.CurrentWorkspace);
        if (curRepo.IsGitRepo)
            curRepo.TryUpdateUrl(modules.FirstOrDefault(m => m.Name.Equals(moduleName)));
        HooksHelper.InstallHooks(moduleName);

        var getter = new ModuleGetter(
            consoleWriter, cycleDetector, depsValidatorFactory, gitRepositoryFactory, Helper.GetModules(),
            new Dep(moduleName, null, configuration), options.Policy, options.MergedBranch, options.Verbose,
            options.LocalBranchForce, options.GitDepth);

        getter.GetDeps();

        Log.LogInformation("SUCCESS UPDATE DEPS");
        return 0;
    }
}
