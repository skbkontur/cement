using System.IO;
using Common;
using Common.DepsValidators;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Commands;

[PublicAPI]
public sealed class UpdateCommand : Command<UpdateCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "update",
        Location = CommandLocation.RootModuleDirectory
    };

    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public UpdateCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, CycleDetector cycleDetector,
                         IDepsValidatorFactory depsValidatorFactory, IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.cycleDetector = cycleDetector;
        this.depsValidatorFactory = depsValidatorFactory;
        this.gitRepositoryFactory = gitRepositoryFactory;
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
        Log.LogInformation("Updating packages");
        PackageUpdater.Shared.UpdatePackages();
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
        var parsedArgs = ArgumentParser.ParseUpdate(args);
        var treeish = (string)parsedArgs["treeish"];
        var verbose = (bool)parsedArgs["verbose"];
        var policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);
        var gitDepth = (int?)parsedArgs["gitDepth"];

        return new UpdateCommandOptions(treeish, verbose, policy, gitDepth);
    }
}
