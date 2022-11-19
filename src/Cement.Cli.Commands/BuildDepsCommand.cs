using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cement.Cli.Common;
using Cement.Cli.Common.Graph;
using Cement.Cli.Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class BuildDepsCommand : Command<BuildDepsCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        MeasureElapsedTime = true,
        Location = CommandLocation.RootModuleDirectory
    };

    private readonly ILogger<BuildDepsCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly BuildPreparer buildPreparer;

    public BuildDepsCommand(ILogger<BuildDepsCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags,
                            BuildPreparer buildPreparer)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.buildPreparer = buildPreparer;
    }

    public static void TryNugetRestore(ILogger log, ConsoleWriter consoleWriter, List<Dep> modulesToUpdate, ModuleBuilder builder)
    {
        log.LogDebug("Restoring NuGet packages");
        consoleWriter.ResetProgress();

        try
        {
            var nugetRunCommand = NuGetHelper.Shared.GetNugetRunCommand();
            if (nugetRunCommand == null)
                return;

            var deps = modulesToUpdate.GroupBy(d => d.Name).ToList();
            Parallel.ForEach(
                deps, Helper.ParallelOptions, group =>
                {
                    consoleWriter.WriteProgress($"{group.Key,-30} nuget restoring");
                    builder.NugetRestore(group.Key, group.Select(d => d.Configuration).ToList(), nugetRunCommand);
                    consoleWriter.SaveToProcessedModules(group.Key);
                });
        }
        catch (AggregateException ae)
        {
            log.LogError(ae.Flatten().InnerExceptions.First(), ae.Flatten().InnerExceptions.First().Message);
        }
        catch (Exception e)
        {
            log.LogError(e, e.Message);
        }

        log.LogDebug("OK NuGet packages restored");
        consoleWriter.ResetProgress();
    }

    public override string Name => "build-deps";
    public override string HelpMessage => @"
    Performs build for current module dependencies

    Usage:
        cm build-deps [-r|--rebuild] [-q|--quickly] [-v|--verbose|-w|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -r/--rebuild              - rebuild all deps (default skip module if it was already built,
                                    according to its commit-hash)
        -q/--quickly              - build deps in parallel
        -c/--configuration        - build deps for corresponding configuration

        -v/--verbose              - show full msbuild output
        -w/--warnings             - show warnings

        -p/--progress             - show msbuild output in one line
        --cleanBeforeBuild        - delete all local changes if project's TargetFramework is 'netstandardXX'
";

    protected override BuildDepsCommandOptions ParseArgs(string[] args)
    {
        Helper.RemoveOldKey(ref args, "-t", logger);

        var parsedArgs = ArgumentParser.ParseBuildDeps(args);
        var configuration = (string)parsedArgs["configuration"];
        var rebuild = (bool)parsedArgs["rebuild"];
        var parallel = (bool)parsedArgs["quickly"];
        var buildSettings = new BuildSettings
        {
            ShowAllWarnings = (bool)parsedArgs["warnings"],
            ShowOutput = (bool)parsedArgs["verbose"],
            ShowProgress = (bool)parsedArgs["progress"],
            CleanBeforeBuild = (bool)parsedArgs["cleanBeforeBuild"]
        };

        return new BuildDepsCommandOptions(configuration, rebuild, parallel, buildSettings);
    }

    protected override int Execute(BuildDepsCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();
        var moduleName = Path.GetFileName(cwd);

        var configuration = string.IsNullOrEmpty(options.Configuration) ? "full-build" : options.Configuration;
        var buildSettings = options.BuildSettings;

        var cleanerLogger = LogManager.GetLogger<Cleaner>();
        var shellRunner = new ShellRunner(LogManager.GetLogger<ShellRunner>());
        var cleaner = new Cleaner(cleanerLogger, shellRunner, consoleWriter);
        var buildYamlScriptsMaker = new BuildYamlScriptsMaker();
        var builder = new ModuleBuilder(logger, consoleWriter, buildSettings, buildYamlScriptsMaker);
        var builderInitTask = Task.Run(() => builder.Init());
        var modulesOrder = buildPreparer.GetModulesOrder(moduleName, configuration ?? "full-build");
        var modulesToBuild = modulesOrder.UpdatedModules;

        if (options.Rebuild)
            modulesToBuild = modulesOrder.BuildOrder.ToList();

        if (modulesToBuild.Count > 0 && modulesToBuild[modulesToBuild.Count - 1].Name == moduleName)
        {
            modulesToBuild.RemoveAt(modulesToBuild.Count - 1); //remove root
        }

        var builtStorage = BuildInfoStorage.Deserialize();
        foreach (var dep in modulesToBuild)
            builtStorage.RemoveBuildInfo(dep.Name);

        builderInitTask.Wait();

        if (FeatureFlags.CleanBeforeBuild || buildSettings.CleanBeforeBuild)
            TryCleanModules(modulesToBuild, cleaner);

        TryNugetRestore(logger, consoleWriter, modulesToBuild, builder);

        var isSuccessful = options.Parallel
            ? BuildDepsParallel(modulesOrder, builtStorage, modulesToBuild, builder)
            : BuildDepsSequential(modulesOrder, builtStorage, modulesToBuild, builder);

        return isSuccessful ? 0 : -1;
    }

    private bool BuildDepsSequential(ModulesOrder modulesOrder, BuildInfoStorage buildStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
    {
        var built = 1;
        for (var i = 0; i < modulesOrder.BuildOrder.Count - 1; i++)
        {
            var dep = modulesOrder.BuildOrder[i];

            if (NoNeedToBuild(dep, modulesToBuild))
            {
                buildStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                continue;
            }

            consoleWriter.WriteProgress($"{dep.ToBuildString(),-49} {$"{built}/{modulesToBuild.Count}",10}");
            try
            {
                if (!builder.Build(dep))
                {
                    buildStorage.Save();
                    return false;
                }
            }
            catch (Exception)
            {
                buildStorage.Save();
                throw;
            }

            buildStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
            built++;
        }

        buildStorage.Save();
        logger.LogDebug("msbuild time: " + new TimeSpan(ModuleBuilder.TotalMsbuildTime));
        return true;
    }

    private bool BuildDepsParallel(ModulesOrder modulesOrder, BuildInfoStorage buildStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
    {
        var logger = LogManager.GetLogger<ParallelBuilder>();
        var graphHelper = new GraphHelper();

        var parallelBuilder = new ParallelBuilder(logger, graphHelper);
        parallelBuilder.Initialize(modulesOrder.ConfigsGraph);

        var tasks = new List<Task>();
        var builtCount = 1;

        for (var i = 0; i < Helper.MaxDegreeOfParallelism; i++)
        {
            tasks.Add(
                Task.Run(
                    () =>
                    {
                        while (true)
                        {
                            var dep = parallelBuilder.TryStartBuild();
                            if (dep == null)
                                return;

                            if (dep.Equals(modulesOrder.BuildOrder.LastOrDefault()))
                            {
                                parallelBuilder.EndBuild(dep);
                                continue;
                            }

                            if (NoNeedToBuild(dep, modulesToBuild))
                            {
                                parallelBuilder.EndBuild(dep);

                                lock (buildStorage)
                                    buildStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                                continue;
                            }

                            consoleWriter.WriteProgress($"{dep.ToBuildString(),-49} {$"{builtCount}/{modulesToBuild.Count}",10}");
                            var success = builder.Build(dep);

                            parallelBuilder.EndBuild(dep, !success);

                            if (success)
                                lock (buildStorage)
                                {
                                    buildStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                                    builtCount++;
                                }
                        }
                    }));
        }

        Task.WaitAll(tasks.ToArray());

        buildStorage.Save();
        this.logger.LogDebug("msbuild time: " + new TimeSpan(ModuleBuilder.TotalMsbuildTime));
        return !parallelBuilder.IsFailed;
    }

    private bool NoNeedToBuild(Dep dep, List<Dep> modulesToBuild)
    {
        if (!modulesToBuild.Contains(dep))
        {
            logger.LogDebug($"{dep.ToBuildString(),-40} *build skipped");
            consoleWriter.WriteSkip($"{dep.ToBuildString(),-40}");
            return true;
        }

        return false;
    }

    private void TryCleanModules(List<Dep> modules, Cleaner cleaner)
    {
        foreach (var module in modules)
        {
            if (cleaner.IsNetStandard(module))
                cleaner.Clean(module);
        }
    }
}
