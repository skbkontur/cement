using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Common
{
    public sealed class ModuleBuilder
    {
        public static long TotalMsbuildTime;
        private readonly ConsoleWriter consoleWriter;
        private readonly ILogger log;
        private readonly BuildSettings buildSettings;
        private readonly BuildYamlScriptsMaker buildYamlScriptsMaker;

        public ModuleBuilder(ConsoleWriter consoleWriter, ILogger log, BuildSettings buildSettings,
                             BuildYamlScriptsMaker buildYamlScriptsMaker)
        {
            this.consoleWriter = consoleWriter;
            this.log = log;
            this.buildSettings = buildSettings;
            this.buildYamlScriptsMaker = buildYamlScriptsMaker;
        }

        public void Init()
        {
            if (!Platform.IsUnix())
            {
                VsDevHelper.ReplaceVariablesToVs();
                ModuleBuilderHelper.FindMsBuildsWindows();
                ModuleBuilderHelper.KillMsBuild(log);
            }
        }

        public bool DotnetPack(string directory, string projectFileName, string buildConfiguration)
        {
            var runner = PrepareShellRunner();
            var exitCode = runner.RunInDirectory(directory, $"dotnet pack \\\"{projectFileName}\\\" -c {buildConfiguration}");
            consoleWriter.Write(runner.Output);
            if (exitCode == 0)
                return true;
            log.LogWarning($"Failed to build nuget package {projectFileName}. \nOutput: \n{runner.Output} \nError: \n{runner.Errors} \nExit code: {exitCode}");
            return false;
        }

        public void NugetRestore(string moduleName, List<string> configurations, string nugetRunCommand)
        {
            if (Yaml.Exists(moduleName))
            {
                var buildSections = configurations.SelectMany(c => Yaml.BuildParser(moduleName).Get(c)).ToList();
                var targets = new HashSet<string>();
                foreach (var buildSection in buildSections)
                {
                    if (buildSection.Target == null || !buildSection.Target.EndsWith(".sln"))
                        continue;
                    if (buildSection.Tool.Name != ToolNames.DOTNET)
                    {
                        var target = Path.Combine(Helper.CurrentWorkspace, moduleName, buildSection.Target);
                        targets.Add(target);
                    }
                }

                foreach (var target in targets)
                {
                    RunNugetRestore(target, nugetRunCommand);
                }
            }
            else
                RunNugetRestore(Path.Combine(Helper.CurrentWorkspace, moduleName, "build.cmd"), nugetRunCommand);
        }

        public bool Build(Dep dep)
        {
            try
            {
                log.LogDebug($"{dep.ToBuildString()}");
                if (BuildSingleModule(dep))
                    return true;
                log.LogDebug($"{dep.ToBuildString(),-40} *build failed");
                return false;
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                consoleWriter.WriteError($"Failed to buid {dep}.\n{e}");
                return false;
            }
        }

        private void PrintBuildFailResult(Dep dep, string buildName, BuildScriptWithBuildData script, ShellRunner runner)
        {
            consoleWriter.WriteBuildError(
                $"Failed to build {dep.Name}{(dep.Configuration == null ? "" : "/" + dep.Configuration)} {buildName}");
            foreach (var line in runner.Output.Split('\n'))
                ModuleBuilderHelper.WriteLine(line);

            consoleWriter.WriteLine();
            consoleWriter.WriteInfo("Errors summary:");
            foreach (var line in runner.Output.Split('\n'))
                ModuleBuilderHelper.WriteIfErrorToStandartStream(line);

            consoleWriter.WriteLine($"({script.Script})");
        }

        private void RunNugetRestore(string buildFile, string nugetRunCommand)
        {
            var buildFolder = Directory.GetParent(buildFile).FullName;
            var target = buildFile.EndsWith(".sln") ? Path.GetFileName(buildFile) : "";
            var command = $"{nugetRunCommand} restore {target} -Verbosity {(buildSettings.ShowOutput ? "normal" : "quiet")}";
            log.LogInformation(command);

            var runner = PrepareShellRunner();
            var exitCode = runner.RunInDirectory(buildFolder, command);
            if (exitCode != 0)
            {
                log.LogWarning($"Failed to nuget restore {buildFile}. \nOutput: \n{runner.Output} \nError: \n{runner.Errors} \nExit code: {exitCode}");
            }
        }

        private bool BuildSingleModule(Dep dep)
        {
            var moduleYaml = Path.Combine(Helper.CurrentWorkspace, dep.Name, Helper.YamlSpecFile);
            var cmdFile = Path.Combine(Helper.CurrentWorkspace, ModuleBuilderHelper.GetBuildScriptName(dep));

            if (!Build(dep, moduleYaml, cmdFile))
                return false;

            CheckHasInstall(dep);
            return true;
        }

        private void CheckHasInstall(Dep dep)
        {
            if (!Yaml.Exists(dep.Name))
                return;

            var artifacts = Yaml.InstallParser(dep.Name).Get(dep.Configuration).Artifacts;
            foreach (var artifact in artifacts)
            {
                var fixedPath = Helper.FixPath(artifact);
                if (!File.Exists(Path.Combine(Helper.CurrentWorkspace, dep.Name, fixedPath)))
                {
                    consoleWriter.WriteError($"{artifact} not found in {dep.Name}. Check install section.");
                    log.LogWarning($"{artifact} not found in {dep.Name}");
                }
            }
        }

        private bool Build(Dep dep, string moduleYaml, string cmdFile)
        {
            if (File.Exists(moduleYaml))
            {
                var scripts = buildYamlScriptsMaker.PrepareBuildScriptsFromYaml(dep);
                if (scripts.Any(script => script != null))
                    return scripts.All(script => RunBuildScript(dep, script));
            }

            if (File.Exists(cmdFile))
            {
                return BuildByCmd(dep, cmdFile);
            }

            consoleWriter.WriteSkip($"{dep.ToBuildString(),-40}*content");
            return true;
        }

        private bool BuildByCmd(Dep dep, string cmdFile)
        {
            return RunBuildScript(dep, new BuildScriptWithBuildData(cmdFile, null));
        }

        private bool RunBuildScript(Dep dep, BuildScriptWithBuildData script)
        {
            var sw = Stopwatch.StartNew();
            var command = script.Script;
            var runner = PrepareShellRunner();

            var exitCode = -1;
            for (var timesTry = 0; timesTry < 2 && exitCode != 0; timesTry++)
            {
                log.LogDebug("Build command: '{0}'", command);
                if (buildSettings.ShowOutput)
                    consoleWriter.WriteInfo($"BUILDING {command}");
                exitCode = runner.RunInDirectory(Path.Combine(Helper.CurrentWorkspace, dep.Name), command, TimeSpan.FromMinutes(60));
            }

            sw.Stop();
            Interlocked.Add(ref TotalMsbuildTime, sw.ElapsedTicks);

            var elapsedTime = Helper.ConvertTime(sw.ElapsedMilliseconds);
            var warnCount = runner.Output.Split('\n').Count(ModuleBuilderHelper.IsWarning);
            var obsoleteUsages = runner.Output.Split('\n').Where(ModuleBuilderHelper.IsObsoleteWarning).ToList();

            var buildName = script.BuildData?.Name ?? "";
            if (exitCode != 0)
            {
                PrintBuildFailResult(dep, buildName, script, runner);
                return false;
            }

            PrintBuildResult(dep, buildName, warnCount, elapsedTime, obsoleteUsages);
            return true;
        }

        private void PrintBuildResult(Dep dep, string buildName, int warnCount, string elapsedTime, List<string> obsoleteUsages)
        {
            consoleWriter.WriteOk(
                $"{dep.ToBuildString() + " " + buildName,-40}{(warnCount == 0 || buildSettings.ShowWarningsSummary ? "" : "warnings: " + warnCount),-15}{elapsedTime,10}");

            var obsoleteCount = obsoleteUsages.Count;
            if (buildSettings.ShowWarningsSummary && warnCount > 0)
            {
                consoleWriter.WriteBuildWarning(
                    $"       warnings: {warnCount}{(obsoleteCount == 0 ? "" : ", obsolete usages: " + obsoleteCount)} (Use -w key to print warnings or -W to print obsolete usages. You can also use ReSharper to find them.)");
            }
        }

        private ShellRunner PrepareShellRunner()
        {
            var runner = new ShellRunner();
            if (buildSettings.ShowOutput)
            {
                runner.OnOutputChange += ModuleBuilderHelper.WriteLine;
                return runner;
            }

            if (buildSettings.ShowProgress)
                runner.OnOutputChange += ModuleBuilderHelper.WriteProgress;

            if (buildSettings.ShowAllWarnings)
            {
                runner.OnOutputChange += ModuleBuilderHelper.WriteIfWarning;
                return runner;
            }

            if (buildSettings.ShowObsoleteWarnings)
            {
                runner.OnOutputChange += ModuleBuilderHelper.WriteIfObsoleteFull;
                return runner;
            }

            if (buildSettings.ShowWarningsSummary)
                runner.OnOutputChange += ModuleBuilderHelper.WriteIfObsoleteGrouped;
            return runner;
        }
    }
}
