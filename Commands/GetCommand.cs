using System.IO;
using Common;
using Common.DepsValidators;
using Common.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Commands;

[PublicAPI]
public sealed class GetCommand : Command<GetCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "get",
        MeasureElapsedTime = true,
        Location = CommandLocation.WorkspaceDirectory
    };

    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public GetCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, CycleDetector cycleDetector,
                      IDepsValidatorFactory depsValidatorFactory, IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.cycleDetector = cycleDetector;
        this.depsValidatorFactory = depsValidatorFactory;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

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
        Helper.RemoveOldKey(ref args, "-n", Log);

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

        Log.LogInformation("Updating packages");
        PackageUpdater.Shared.UpdatePackages();

        var getter = new ModuleGetter(
            consoleWriter, cycleDetector, depsValidatorFactory, gitRepositoryFactory, Helper.GetModules(),
            new Dep(module, options.Treeish, configuration), options.Policy, options.MergedBranch, options.Verbose,
            gitDepth: options.GitDepth);

        getter.GetModule();

        consoleWriter.WriteInfo("Getting deps for " + module);
        Log.LogInformation("Getting deps list for " + module);

        getter.GetDeps();

        cycleDetector.WarnIfCycle(module, configuration, Log);

        Log.LogInformation("SUCCESS get " + module);
        return 0;
    }
}
