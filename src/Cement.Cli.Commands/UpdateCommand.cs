using System.IO;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UpdateCommand : Command<UpdateCommandOptions>
{
    private readonly ILogger<UpdateCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly HooksHelper hooksHelper;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly IPackageUpdater packageUpdater;

    public UpdateCommand(ILogger<UpdateCommand> logger, ConsoleWriter consoleWriter, CycleDetector cycleDetector,
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

    public override string Name => "update";
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

    protected override int Execute(UpdateCommandOptions options)
    {
        CommandHelper.SetWorkspace(CommandLocation.RootModuleDirectory);

        logger.LogInformation("Updating packages");
        packageUpdater.UpdatePackages();
        var cwd = Directory.GetCurrentDirectory();
        var module = Path.GetFileName(cwd);

        var curRepo = gitRepositoryFactory.Create(module, Helper.CurrentWorkspace);

        var treeish = options.Treeish;
        if (treeish == null)
            treeish = curRepo.CurrentLocalTreeish().Value;

        var getter = new ModuleGetter(
            consoleWriter,
            cycleDetector,
            depsValidatorFactory,
            gitRepositoryFactory,
            hooksHelper,
            Helper.GetModules(),
            new Dep(module, treeish),
            options.Policy,
            null,
            options.Verbose,
            gitDepth: options.GitDepth);

        getter.GetModule();

        return 0;
    }

    protected override UpdateCommandOptions ParseArgs(string[] args)
    {
        return new UpdateCommandOptionsParser().Parse(args);
    }
}
