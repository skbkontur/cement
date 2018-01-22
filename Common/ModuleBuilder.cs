using Common.YamlParsers;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Common
{
    public class ModuleBuilder
    {
        private readonly ILog log;
        public static TimeSpan TotalMsbuildTime = TimeSpan.Zero;
        private readonly BuildSettings buildSettings;

        public ModuleBuilder(ILog log, BuildSettings buildSettings)
        {
            this.log = log;
            this.buildSettings = buildSettings;
            VsDevHelper.ReplaceVariablesToVs();
        }

        public void NugetRestore(Dep dep, string nuGetPath)
        {
            if (Yaml.Exists(dep.Name))
            {
                var buildSections = Yaml.BuildParser(dep.Name).Get(dep.Configuration);
                foreach (var buildSection in buildSections)
                {
                    if (buildSection.Target == null || !buildSection.Target.EndsWith(".sln"))
                        continue;
                    var target = Path.Combine(Helper.CurrentWorkspace, dep.Name, buildSection.Target);
                    RunNugetRestore(target, nuGetPath);
                }
            }
            else
                RunNugetRestore(Path.Combine(Helper.CurrentWorkspace, dep.Name, "build.cmd"), nuGetPath);
        }

        private void RunNugetRestore(string buildFile, string nuGetPath)
        {
            var buildFolder = Directory.GetParent(buildFile).FullName;
            var target = buildFile.EndsWith(".sln") ? Path.GetFileName(buildFile) : "";
            var command = $"\"{nuGetPath}\" restore {target} -Verbosity {(buildSettings.ShowOutput ? "normal" : "quiet")}";
            if (Helper.OsIsUnix())
                command = $"mono {command}";
            log.Info(command);

            var runner = PrepareShellRunner();
            var exitCode = runner.RunInDirectory(buildFolder, command);
            if (exitCode != 0)
            {
                log.Warn($"Failed to nuget restore {buildFile}. \nOutput: \n{runner.Output} \nError: \n{runner.Errors} \nExit code: {exitCode}");
            }
        }

        public bool Build(Dep dep)
        {
            log.Debug($"{dep.ToBuildString()}");
            if (BuildSingleModule(dep))
                return true;
            log.Debug($"{dep.ToBuildString(),-40} *build failed");
            return false;
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
                    ConsoleWriter.WriteError($"{artifact} not found in {dep.Name}. Check install section.");
                    log.Warn($"{artifact} not found in {dep.Name}");
                }
            }
        }

        private bool Build(Dep dep, string moduleYaml, string cmdFile)
        {
            if (File.Exists(moduleYaml))
            {
                var scripts = BuildYamlScriptsMaker.PrepareBuildScriptsFromYaml(dep);
                if (scripts.Any(script => script != null))
                    return scripts.All(script => RunBuildScript(dep, script));
            }
            if (File.Exists(cmdFile))
            {
                return BuildByCmd(dep, cmdFile);
            }
            ConsoleWriter.WriteSkip($"{dep.ToBuildString(),-40}*content");
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

            int exitCode = -1;
            for (int timesTry = 0; timesTry < 2 && exitCode != 0; timesTry++)
            {
                ModuleBuilderHelper.KillMsBuild(log);
                log.DebugFormat("Build command: '{0}'", command);
                if (buildSettings.ShowOutput)
                    ConsoleWriter.WriteInfo($"BUILDING {command}");
                exitCode = runner.RunInDirectory(Path.Combine(Helper.CurrentWorkspace, dep.Name), command, TimeSpan.FromMinutes(60));
            }

            sw.Stop();
            TotalMsbuildTime += sw.Elapsed;

            var elapsedTime = Helper.ConvertTime(sw.ElapsedMilliseconds);
            var warnCount = runner.Output.Split('\n').Count(ModuleBuilderHelper.IsWarning);
            var obsoleteUsages = runner.Output.Split('\n').Where(ModuleBuilderHelper.IsObsoleteWarning).ToList();

            var buildName = script.BuildData == null ? "" : script.BuildData.Name;
            if (exitCode != 0)
            {
                PrintBuildFailResult(dep, buildName, script, runner);
                return false;
            }

            PrintBuildResult(dep, buildName, warnCount, elapsedTime, obsoleteUsages);
            return true;
        }

        private static void PrintBuildFailResult(Dep dep, string buildName, BuildScriptWithBuildData script, ShellRunner runner)
        {
            ConsoleWriter.WriteBuildError(
                $"Failed to build {dep.Name}{(dep.Configuration == null ? "" : "/" + dep.Configuration)} {buildName}");
            foreach (var line in runner.Output.Split('\n'))
                ModuleBuilderHelper.WriteLine(line);

            ConsoleWriter.WriteLine();
            ConsoleWriter.WriteInfo("Errors summary:");
            foreach (var line in runner.Output.Split('\n'))
                ModuleBuilderHelper.WriteIfErrorToStandartStream(line);

            ConsoleWriter.WriteLine($"({script.Script})");
        }

        private void PrintBuildResult(Dep dep, string buildName, int warnCount, string elapsedTime, List<string> obsoleteUsages)
        {
            ConsoleWriter.WriteOk(
                $"{dep.ToBuildString() + " " + buildName,-40}{(warnCount == 0 || buildSettings.ShowWarningsSummary ? "" : "warnings: " + warnCount),-15}{elapsedTime,10}");

            var obsoleteCount = obsoleteUsages.Count;
            if (buildSettings.ShowWarningsSummary && warnCount > 0)
            {
                ConsoleWriter.WriteBuildWarning(
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

    public class BuildScriptWithBuildData
    {
        public readonly string Script;
        public readonly BuildData BuildData;

        public BuildScriptWithBuildData(string script, BuildData buildData)
        {
            Script = script;
            BuildData = buildData;
        }
    }
}