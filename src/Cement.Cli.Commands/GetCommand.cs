using System.IO;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class GetCommand : Command<GetCommandOptions>
{
    private readonly ILogger<GetCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly HooksHelper hooksHelper;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly IPackageUpdater packageUpdater;

    public GetCommand(ILogger<GetCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags,
                      CycleDetector cycleDetector, IDepsValidatorFactory depsValidatorFactory, HooksHelper hooksHelper,
                      IGitRepositoryFactory gitRepositoryFactory, IPackageUpdater packageUpdater)
        : base(consoleWriter, featureFlags)
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
    public override CommandLocation Location { get; set; } = CommandLocation.WorkspaceDirectory;

    public override string Name => "get";
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

    protected override GetCommandOptions ParseArgs(string[] args)
    {
        Helper.RemoveOldKey(ref args, "-n", logger);

        var parsedArgs = ArgumentParser.ParseGet(args);
        var module = (string)parsedArgs["module"];
        if (string.IsNullOrEmpty(module))
            throw new CementException("You should specify the name of the module");

        var treeish = (string)parsedArgs["treeish"];
        var configuration = (string)parsedArgs["configuration"];
        var mergedBranch = (string)parsedArgs["merged"];
        var verbose = (bool)parsedArgs["verbose"];
        var gitDepth = (int?)parsedArgs["gitDepth"];
        var policy = PolicyMapper.GetLocalChangesPolicy(parsedArgs);

        return new GetCommandOptions(configuration, policy, module, treeish, mergedBranch, verbose, gitDepth);
    }

    protected override int Execute(GetCommandOptions options)
    {
        var module = options.Module;

        var workspace = Directory.GetCurrentDirectory();
        if (!Helper.IsCurrentDirectoryModule(Path.Combine(workspace, module)))
            throw new CementTrackException($"{workspace} is not cement workspace directory.");

        var configuration = string.IsNullOrEmpty(options.Configuration) ? "full-build" : options.Configuration;

        logger.LogInformation("Updating packages");
        packageUpdater.UpdatePackages();

        var getter = new ModuleGetter(
            consoleWriter, cycleDetector, depsValidatorFactory, gitRepositoryFactory, hooksHelper, Helper.GetModules(),
            new Dep(module, options.Treeish, configuration), options.Policy, options.MergedBranch, options.Verbose,
            gitDepth: options.GitDepth);

        getter.GetModule();

        consoleWriter.WriteInfo("Getting deps for " + module);
        logger.LogInformation("Getting deps list for " + module);

        getter.GetDeps();

        cycleDetector.WarnIfCycle(module, configuration, logger);

        logger.LogInformation("SUCCESS get " + module);
        return 0;
    }
}
