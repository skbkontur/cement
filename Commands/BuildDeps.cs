using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Extensions;
using Common.Graph;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public class BuildDeps : Command
    {
        private string configuration;
        private bool rebuild;
        private bool parallel;
        private BuildSettings buildSettings;

        public BuildDeps()
            : base(new CommandSettings
            {
                LogPerfix = "BUILD-DEPS",
                LogFileName = "build-deps",
                MeasureElapsedTime = true,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            Helper.RemoveOldKey(ref args, "-t", Log);

            var parsedArgs = ArgumentParser.ParseBuildDeps(args);
            configuration = (string) parsedArgs["configuration"];
            rebuild = (bool) parsedArgs["rebuild"];
            parallel = (bool)parsedArgs["quickly"];
            buildSettings = new BuildSettings
            {
                ShowAllWarnings = (bool) parsedArgs["warnings"],
                ShowOutput = (bool) parsedArgs["verbose"],
                ShowProgress = (bool) parsedArgs["progress"],
                ClearBeforeBuild = (bool) parsedArgs["clear"]
            };
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;
            
            var builder = new ModuleBuilder(Log, buildSettings);
            var builderInitTask = Task.Run(() => builder.Init());
            var modulesOrder = new BuildPreparer(Log).GetModulesOrder(moduleName, configuration ?? "full-build");
            var modulesToBuild = modulesOrder.UpdatedModules;

            if (rebuild)
                modulesToBuild = modulesOrder.BuildOrder.ToList();

            if (modulesToBuild.Count > 0 && modulesToBuild[modulesToBuild.Count - 1].Name == moduleName)
            {
                modulesToBuild.RemoveAt(modulesToBuild.Count - 1); //remove root
            }

            var builtStorage = BuiltInfoStorage.Deserialize();
            foreach (var dep in modulesToBuild)
                builtStorage.RemoveBuildInfo(dep.Name);

            builderInitTask.Wait();
            TryNugetRestore(modulesToBuild, builder);

            var isSuccessful = parallel ?
                BuildDepsParallel(modulesOrder, builtStorage, modulesToBuild, builder) :
                BuildDepsSequential(modulesOrder, builtStorage, modulesToBuild, builder);
            return isSuccessful ? 0 : -1;
        }

        private static bool BuildDepsSequential(ModulesOrder modulesOrder, BuiltInfoStorage builtStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
        {
            var built = 1;
            for (var i = 0; i < modulesOrder.BuildOrder.Count - 1; i++)
            {
                var dep = modulesOrder.BuildOrder[i];

                if (NoNeedToBuild(dep, modulesToBuild))
                {
                    builtStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                    continue;
                }

                ConsoleWriter.WriteProgress($"{dep.ToBuildString(),-49} {$"{built}/{modulesToBuild.Count}",10}");
                try
                {
                    if (!builder.Build(dep))
                    {
                        builtStorage.Save();
                        return false;
                    }
                }
                catch (Exception)
                {
                    builtStorage.Save();
                    throw;
                }

                builtStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                built++;
            }
            builtStorage.Save();
            Log.LogDebug("msbuild time: " + new TimeSpan(ModuleBuilder.TotalMsbuildTime));
            return true;
        }

        private static bool BuildDepsParallel(ModulesOrder modulesOrder, BuiltInfoStorage builtStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
        {
            var parallelBuilder = new ParallelBuilder(modulesOrder.ConfigsGraph);
            var tasks = new List<Task>();
            var builtCount = 1;

            for (int i = 0; i < Helper.MaxDegreeOfParallelism; i++)
            {
                tasks.Add(Task.Run(() =>
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

                            lock (builtStorage)
                                builtStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                            continue;
                        }

                        ConsoleWriter.WriteProgress($"{dep.ToBuildString(),-49} {$"{builtCount}/{modulesToBuild.Count}",10}");
                        var success = builder.Build(dep);

                        parallelBuilder.EndBuild(dep, !success);

                        if (success)
                            lock (builtStorage)
                            {
                                builtStorage.AddBuiltModule(dep, modulesOrder.CurrentCommitHashes);
                                builtCount++;
                            }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            builtStorage.Save();
            Log.LogDebug("msbuild time: " + new TimeSpan(ModuleBuilder.TotalMsbuildTime));
            return !parallelBuilder.IsFailed;
        }

        public static void TryNugetRestore(List<Dep> modulesToUpdate, ModuleBuilder builder)
        {
            Log.LogDebug("Restoring NuGet packages");
            ConsoleWriter.ResetProgress();
            try
            {
                var nugetRunCommand = NuGetHelper.GetNugetRunCommand();
                if (nugetRunCommand == null)
                    return;

                var deps = modulesToUpdate.GroupBy(d => d.Name).ToList();
                Parallel.ForEach(deps, Helper.ParallelOptions, group =>
                {
                    ConsoleWriter.WriteProgress($"{group.Key,-30} nuget restoring");
                    builder.NugetRestore(group.Key, group.Select(d => d.Configuration).ToList(), nugetRunCommand);
                    ConsoleWriter.SaveToProcessedModules(group.Key);
                });
            }
            catch (AggregateException ae)
            {
                Log.LogError(ae.Flatten().InnerExceptions.First(), ae.Flatten().InnerExceptions.First().Message);
            }
            catch (Exception e)
            {
                Log.LogError(e, e.Message);
            }
            Log.LogDebug("OK NuGet packages restored");
            ConsoleWriter.ResetProgress();
        }

        private static bool NoNeedToBuild(Dep dep, List<Dep> modulesToBuild)
        {
            if (!modulesToBuild.Contains(dep))
            {
                Log.LogDebug($"{dep.ToBuildString(),-40} *build skipped");
                ConsoleWriter.WriteSkip($"{dep.ToBuildString(),-40}");
                return true;
            }
            return false;
        }

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
        --clear                   - remove 'bin' and 'obj' folders before build
";
    }
}