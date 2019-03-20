using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;

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
                LogFileName = "build-deps.net.log",
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
                ShowProgress = (bool) parsedArgs["progress"]
            };
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;
            
            var builder = new ModuleBuilder(Log, buildSettings);
            var builderInitTask = Task.Run(() => builder.Init());
            var modulesOrder = new BuildPreparer(Log).GetModulesOrder(moduleName, configuration ?? "full-build", parallel);
            var modulesToBuild = modulesOrder.UpdatedModules;
            if (modulesToBuild.Count > 0 && modulesToBuild[modulesToBuild.Count - 1].Name == moduleName)
            {
                modulesToBuild.RemoveAt(modulesToBuild.Count - 1); //remove root
            }
            if (rebuild)
                modulesToBuild = modulesOrder.BuildOrder;

            var builtStorage = BuiltInfoStorage.Deserialize();
            foreach (var dep in modulesToBuild)
                builtStorage.RemoveBuildInfo(dep.Name);

            builderInitTask.Wait();
            TryNugetRestore(modulesToBuild, builder);

            var isSuccessful = parallel ?
                BuildDepsParallel(modulesOrder, builtStorage, modulesToBuild, builder) :
                BuildDepsSequental(modulesOrder, builtStorage, modulesToBuild, builder);
            return isSuccessful ? 0 : -1;
        }

        private static bool BuildDepsSequental(ModulesOrder modulesOrder, BuiltInfoStorage builtStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
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
            Log.Debug("msbuild time: " + ModuleBuilder.TotalMsbuildTime);
            return true;
        }

        private static bool BuildDepsParallel(ModulesOrder modulesOrder, BuiltInfoStorage builtStorage, List<Dep> modulesToBuild, ModuleBuilder builder)
        {
            var built = 1;
            var isFailed = false;
            var buildedDepsNames = new HashSet<string>();
            Exception exception = null;
            var semaphore = new SemaphoreSlim(Helper.MaxDegreeOfParallelism);
            var tasks = new List<Task>();

            for (var i = 0; i < modulesOrder.BuildOrder.Count - 1; i++)
            {
                var module = modulesOrder.BuildOrder[i];
                lock (buildedDepsNames)
                {
                    if (NoNeedToBuild(module, modulesToBuild))
                    {
                        builtStorage.AddBuiltModule(module, modulesOrder.CurrentCommitHashes);
                        buildedDepsNames.Add(module.Name);
                        continue;
                    }
                    if (isFailed)
                        break;
                }

                semaphore.Wait();
                Log.Debug($"Waiting for {module.ToBuildString()}");
                
                while (true)
                {
                    lock (buildedDepsNames)
                    {
                        
                        if (isFailed)
                            break;
                        // Corner case: we have already build module/client and now begin build module/full-build. In fact rebuild.
                        // Build process will fail if at the same time begin to build another module dependent on module/client
                        // If we need to build this module twice, wait for all other builds, because they can use our module.
                        // In General, such problems can occur with direct dependencies of the module
                        // But some modules do not specify all their direct dependencies. So we have to look at the whole dependency tree.
                        var allDepsIsBuilded = AllModuleDepsIsBuilded(module, buildedDepsNames, modulesOrder);
                        var isRebuildAlreadyBuildedModule = buildedDepsNames.Contains(module.Name);
                        
                        if (allDepsIsBuilded && isRebuildAlreadyBuildedModule && semaphore.CurrentCount == Helper.MaxDegreeOfParallelism - 1)
                        {
                            buildedDepsNames.Remove(module.Name);
                            break;
                        }
                        if (allDepsIsBuilded && !isRebuildAlreadyBuildedModule)
                            break;

                    }
                    Task.WaitAny(tasks.Where(t => !t.IsCompleted).ToArray());
                }

                if (isFailed)
                {
                    semaphore.Release();
                    break;
                }

                tasks.Add(Task.Run(() =>
                {
                    ConsoleWriter.WriteProgress($"{module.ToBuildString(),-49} {$"{built}/{modulesToBuild.Count}",10}");
                    
                    try
                    {
                        Log.Debug($"Building for {module.ToBuildString()}");
                        if (!builder.Build(module))
                            isFailed = true;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        isFailed = true;
                    }

                    lock (buildedDepsNames)
                    {
                        if (!isFailed)
                        {
                            builtStorage.AddBuiltModule(module, modulesOrder.CurrentCommitHashes);
                            built++;
                            buildedDepsNames.Add(module.Name);
                        }
                    }
                    Log.Debug($"Builded {module.ToBuildString()}");
                    semaphore.Release();
                }));
            }
            Task.WaitAll(tasks.Where(t => !t.IsCompleted).ToArray());
            builtStorage.Save();
            Log.Debug("msbuild time: " + ModuleBuilder.TotalMsbuildTime);
            if (exception != null)
                throw exception;
            return !isFailed;
        }

        private static bool AllModuleDepsIsBuilded(Dep module, HashSet<string> depsNamesToCheck, ModulesOrder modulesOrder)
        {
            var moduleDeps = modulesOrder.ConfigsGraph[module];
            foreach (var dep in moduleDeps)
            {
                if (!depsNamesToCheck.Contains(dep.Name)) return false;
                if (!AllModuleDepsIsBuilded(dep, depsNamesToCheck, modulesOrder)) return false;
            }
            return true;
        }

        public static void TryNugetRestore(List<Dep> modulesToUpdate, ModuleBuilder builder)
        {
            Log.Debug("Restoring NuGet packages");
            ConsoleWriter.ResetProgress();
            try
            {
                var nugetRunCommand = NuGetHelper.GetNugetRunCommand();
                if (nugetRunCommand == null)
                    return;

                var uniqueDeps = modulesToUpdate.GroupBy(d => d.Name).Select(g => g.First()).ToList();
                Parallel.ForEach(uniqueDeps, Helper.ParallelOptions, dep =>
                {
                    ConsoleWriter.WriteProgress($"{dep.Name,-30} nuget restoring");
                    builder.NugetRestore(dep, nugetRunCommand);
                    ConsoleWriter.SaveToProcessedModules(dep.Name);
                });
            }
            catch (AggregateException ae)
            {
                Log.Error(ae.Flatten().InnerExceptions.First());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            Log.Debug("OK NuGet packages restored");
            ConsoleWriter.ResetProgress();
        }

        private static bool NoNeedToBuild(Dep dep, List<Dep> modulesToUpdate)
        {
            if (!modulesToUpdate.Contains(dep))
            {
                Log.Debug($"{dep.ToBuildString(),-40} *build skipped");
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
";
    }
}