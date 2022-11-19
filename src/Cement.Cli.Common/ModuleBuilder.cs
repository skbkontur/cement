using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Cement.Cli.Common.Logging;
using Cement.Cli.Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Common;

public sealed class ModuleBuilder
{
    public static long TotalMsbuildTime;

    private readonly ConsoleWriter consoleWriter;
    private readonly ILogger log;
    private readonly BuildSettings buildSettings;
    private readonly BuildYamlScriptsMaker buildYamlScriptsMaker;
    private readonly VsDevHelper vsDevHelper;

    public ModuleBuilder(ILogger log, ConsoleWriter consoleWriter, BuildSettings buildSettings,
                         BuildYamlScriptsMaker buildYamlScriptsMaker)
    {
        this.consoleWriter = consoleWriter;
        this.log = log;
        this.buildSettings = buildSettings;
        this.buildYamlScriptsMaker = buildYamlScriptsMaker;
        vsDevHelper = new VsDevHelper(LogManager.GetLogger<VsDevHelper>());
    }

    public void Init()
    {
        if (Platform.IsUnix())
            return;

        vsDevHelper.ReplaceVariablesToVs();
        ModuleBuilderHelper.Shared.FindMsBuildsWindows();
        ModuleBuilderHelper.Shared.KillMsBuild(log);
    }

    public bool DotnetPack(string directory, string projectFileName, string buildConfiguration)
    {
        var runner = PrepareShellRunner();
        var (exitCode, output, errors) = runner
            .RunInDirectory(directory, $"dotnet pack \\\"{projectFileName}\\\" -c {buildConfiguration}");

        consoleWriter.Write(output);
        if (exitCode == 0)
            return true;

        log.LogWarning(
            $"Failed to build nuget package {projectFileName}.\nOutput: \n{output} \n" +
            $"Error: \n{errors} \nExit code: {exitCode}");

        return false;
    }

    public void NugetRestore(string moduleName, List<string> configurations, string nugetRunCommand)
    {
        if (!Yaml.Exists(moduleName))
        {
            RunNugetRestore(Path.Combine(Helper.CurrentWorkspace, moduleName, "build.cmd"), nugetRunCommand);
            return;
        }

        var buildSections = configurations.SelectMany(c => Yaml.BuildParser(moduleName).Get(c));

        var targets = new HashSet<string>();
        foreach (var buildSection in buildSections)
        {
            if (buildSection.Target == null || !buildSection.Target.EndsWith(".sln"))
                continue;

            if (buildSection.Tool.Name == ToolNames.DOTNET)
                continue;

            var target = Path.Combine(Helper.CurrentWorkspace, moduleName, buildSection.Target);
            targets.Add(target);
        }

        foreach (var target in targets)
        {
            RunNugetRestore(target, nugetRunCommand);
        }
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

    private void PrintBuildFailResult(Dep dep, string buildName, BuildScriptWithBuildData script, string output)
    {
        consoleWriter.WriteBuildError($"Failed to build {dep} {buildName}");

        foreach (var line in output.Split('\n'))
            ModuleBuilderHelper.Shared.WriteLine(line);

        consoleWriter.WriteLine();
        consoleWriter.WriteInfo("Errors summary:");

        foreach (var line in output.Split('\n'))
            ModuleBuilderHelper.Shared.WriteIfErrorToStandartStream(line);

        consoleWriter.WriteLine($"({script.Script})");
    }

    private void RunNugetRestore(string buildFile, string nugetRunCommand)
    {
        var buildFolder = Directory.GetParent(buildFile).FullName;
        var target = buildFile.EndsWith(".sln") ? Path.GetFileName(buildFile) : "";

        var command = $"{nugetRunCommand} restore {target} -Verbosity {(buildSettings.ShowOutput ? "normal" : "quiet")}";
        log.LogInformation(command);

        var runner = PrepareShellRunner();
        var (exitCode, output, errors) = runner.RunInDirectory(buildFolder, command);
        if (exitCode != 0)
        {
            log.LogWarning(
                $"Failed to nuget restore {buildFile}." +
                $"\nOutput: \n{output} " +
                $"\nError: \n{errors}" +
                $"\nExit code: {exitCode}");
        }
    }

    private bool BuildSingleModule(Dep dep)
    {
        var moduleYaml = Path.Combine(Helper.CurrentWorkspace, dep.Name, Helper.YamlSpecFile);
        var cmdFile = Path.Combine(Helper.CurrentWorkspace, ModuleBuilderHelper.Shared.GetBuildScriptName(dep));

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
        var output = string.Empty;

        for (var timesTry = 0; timesTry < 2 && exitCode != 0; timesTry++)
        {
            log.LogDebug("Build command: '{0}'", command);
            if (buildSettings.ShowOutput)
                consoleWriter.WriteInfo($"BUILDING {command}");
            (exitCode, output, _) = runner
                .RunInDirectory(Path.Combine(Helper.CurrentWorkspace, dep.Name), command, TimeSpan.FromMinutes(60));
        }

        sw.Stop();
        Interlocked.Add(ref TotalMsbuildTime, sw.ElapsedTicks);

        var elapsedTime = Helper.ConvertTime(sw.ElapsedMilliseconds);
        var warnCount = output.Split('\n').Count(ModuleBuilderHelper.Shared.IsWarning);
        var obsoleteUsages = output.Split('\n').Where(ModuleBuilderHelper.Shared.IsObsoleteWarning).ToList();

        var buildName = script.BuildData?.Name ?? "";
        if (exitCode != 0)
        {
            PrintBuildFailResult(dep, buildName, script, output);
            return false;
        }

        PrintBuildResult(dep, buildName, warnCount, elapsedTime, obsoleteUsages);
        return true;
    }

    private void PrintBuildResult(Dep dep, string buildName, int warnCount, string elapsedTime, List<string> obsoleteUsages)
    {
        consoleWriter.WriteOk(
            $"{dep.ToBuildString() + " " + buildName,-40}" +
            $"{(warnCount == 0 || buildSettings.ShowWarningsSummary ? "" : "warnings: " + warnCount),-15}" +
            $"{elapsedTime,10}");

        var obsoleteCount = obsoleteUsages.Count;
        if (buildSettings.ShowWarningsSummary && warnCount > 0)
        {
            consoleWriter.WriteBuildWarning(
                $"       warnings: {warnCount}{(obsoleteCount == 0 ? "" : ", obsolete usages: " + obsoleteCount)} " +
                "(Use -w key to print warnings or -W to print obsolete usages. You can also use ReSharper to find them.)");
        }
    }

    private ShellRunner PrepareShellRunner()
    {
        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);
        if (buildSettings.ShowOutput)
        {
            shellRunner.OnOutputChange += ModuleBuilderHelper.Shared.WriteLine;
            return shellRunner;
        }

        if (buildSettings.ShowProgress)
            shellRunner.OnOutputChange += ModuleBuilderHelper.Shared.WriteProgress;

        if (buildSettings.ShowAllWarnings)
        {
            shellRunner.OnOutputChange += ModuleBuilderHelper.Shared.WriteIfWarning;
            return shellRunner;
        }

        if (buildSettings.ShowObsoleteWarnings)
        {
            shellRunner.OnOutputChange += ModuleBuilderHelper.Shared.WriteIfObsoleteFull;
            return shellRunner;
        }

        if (buildSettings.ShowWarningsSummary)
            shellRunner.OnOutputChange += ModuleBuilderHelper.Shared.WriteIfObsoleteGrouped;

        return shellRunner;
    }
}
