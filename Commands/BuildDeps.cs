using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;

namespace Commands
{
    public class BuildDeps : Command
    {
        private string configuration;
        private bool rebuild, restore;
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
            buildSettings = new BuildSettings
            {
                ShowAllWarnings = (bool) parsedArgs["warnings"],
                ShowOutput = (bool) parsedArgs["verbose"],
                ShowProgress = (bool) parsedArgs["progress"]
            };
            restore = (bool) parsedArgs["restore"];
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);

            configuration = string.IsNullOrEmpty(configuration) ? "full-build" : configuration;

            List<Dep> modulesToBuild;
            List<Dep> topSortedDeps;
            Dictionary<string, string> currentCommitHases;

            new BuildPreparer(Log).GetModulesOrder(moduleName, configuration ?? "full-build", out topSortedDeps, out modulesToBuild, out currentCommitHases);
            if (rebuild)
                modulesToBuild = topSortedDeps;

            var builtStorage = BuiltInfoStorage.Deserialize();
            foreach (var dep in modulesToBuild)
                builtStorage.RemoveBuildInfo(dep.Name);

            var builder = new ModuleBuilder(Log, buildSettings);
            
            TryNugetRestore(modulesToBuild, builder);

            int built = 1;
            for (var i = 0; i < topSortedDeps.Count - 1; i++)
            {
                var dep = topSortedDeps[i];

                if (NoNeedToBuild(dep, modulesToBuild))
                {
                    builtStorage.AddBuiltModule(dep, currentCommitHases);
                    continue;
                }

                ConsoleWriter.WriteProgress($"{dep.ToBuildString(),-49} {$"{built}/{modulesToBuild.Count - 1}",10}");
                try
                {
                    if (!builder.Build(dep))
                    {
                        builtStorage.Save();
                        return -1;
                    }
                }
                catch (Exception)
                {
                    builtStorage.Save();
                    throw;
                }

                builtStorage.AddBuiltModule(dep, currentCommitHases);
                built++;
            }
            builtStorage.Save();

            Log.Debug("msbuild time: " + ModuleBuilder.TotalMsbuildTime);
            return 0;
        }

        public static void TryNugetRestore(List<Dep> modulesToUpdate, ModuleBuilder builder)
        {
            Log.Debug("Restoing nuget packages");
            ConsoleWriter.ResetProgress();
            try
            {
                var uniqueDeps = modulesToUpdate.GroupBy(d => d.Name).Select(g => g.First()).ToList();
                Parallel.ForEach(uniqueDeps, Helper.ParallelOptions, dep =>
                {
                    ConsoleWriter.WriteProgress($"{dep.Name,-30} nuget restoring");
                    builder.NugetRestore(dep);
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
            Log.Debug("OK nuget packages restored");
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
        cm build-deps [-r|--rebuild] [-v|--verbose|-w|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -r/--rebuild              - rebuild all deps (default skip module if it was already built,
                                    according to its commit-hash)
        -c/--configuration        - build deps for corresponding configuration

        -v/--verbose              - show full msbuild output
        -w/--warnings             - show warnings

        -p/--progress             - show msbuild output in one line
";
    }
}