using System.IO;
using System.Linq;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UpdateDepsCommand : Command<UpdateDepsCommandOptions>
{
    private readonly ILogger<UpdateDepsCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly HooksHelper hooksHelper;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly IPackageUpdater packageUpdater;

    public UpdateDepsCommand(ILogger<UpdateDepsCommand> logger, ConsoleWriter consoleWriter, CycleDetector cycleDetector,
                             IDepsValidatorFactory depsValidatorFactory, HooksHelper hooksHelper,
                             IGitRepositoryFactory gitRepositoryFactory, IPackageUpdater packageUpdater)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.cycleDetector = cycleDetector;
        this.depsValidatorFactory = depsValidatorFactory;
        this.hooksHelper = hooksHelper;
        this.gitRepositoryFactory = gitRepositoryFactory;
        this.packageUpdater = packageUpdater;
    }

    public override bool MeasureElapsedTime { get; set; } = true;

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
        return new UpdateDepsCommandOptionsParser().Parse(args);
    }

    protected override int Execute(UpdateDepsCommandOptions options)
    {
        CommandHelper.SetWorkspace(CommandLocation.RootModuleDirectory);

        var cwd = Directory.GetCurrentDirectory();

        var configuration = string.IsNullOrEmpty(options.Configuration) ? "full-build" : options.Configuration;

        logger.LogInformation("Updating packages");
        packageUpdater.UpdatePackages();
        var modules = Helper.GetModules();

        var moduleName = Path.GetFileName(cwd);

        var curRepo = gitRepositoryFactory.Create(moduleName, Helper.CurrentWorkspace);
        if (curRepo.IsGitRepo)
            curRepo.TryUpdateUrl(modules.FirstOrDefault(m => m.Name.Equals(moduleName)));

        hooksHelper.InstallHooks(moduleName);

        var getter = new ModuleGetter(
            consoleWriter, cycleDetector, depsValidatorFactory, gitRepositoryFactory, hooksHelper, Helper.GetModules(),
            new Dep(moduleName, null, configuration), options.Policy, options.MergedBranch, options.Verbose,
            options.LocalBranchForce, options.GitDepth);

        getter.GetDeps();

        logger.LogInformation("SUCCESS UPDATE DEPS");
        return 0;
    }
}
