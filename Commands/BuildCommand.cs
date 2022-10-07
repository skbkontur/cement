using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Logging;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class BuildCommand : Command<BuildCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "build",
        Location = CommandLocation.RootModuleDirectory
    };

    private readonly ConsoleWriter consoleWriter;
    private readonly BuildPreparer buildPreparer;

    public BuildCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, BuildPreparer buildPreparer)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.buildPreparer = buildPreparer;
    }

    public override string Name => "build";
    public override string HelpMessage => @"
    Performs build for the current module

    Usage:
        cm build [-v|--verbose|-w|-W|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -c/--configuration      - build corresponding configuration

        -v/--verbose            - show full msbuild output
        -w/--warnings           - show warnings
        -W                      - show only obsolete warnings

        -p/--progress           - show msbuild output in one line
        --cleanBeforeBuild      - delete all local changes if project's TargetFramework is 'netstandardXX'
";

    protected override BuildCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseBuildDeps(args);
        var configuration = (string)parsedArgs["configuration"];
        var buildSettings = new BuildSettings
        {
            ShowAllWarnings = (bool)parsedArgs["warnings"],
            ShowObsoleteWarnings = (bool)parsedArgs["obsolete"],
            ShowOutput = (bool)parsedArgs["verbose"],
            ShowProgress = (bool)parsedArgs["progress"],
            ShowWarningsSummary = true,
            CleanBeforeBuild = (bool)parsedArgs["cleanBeforeBuild"]
        };

        return new BuildCommandOptions(configuration, buildSettings);
    }

    protected override int Execute(BuildCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();
        var moduleName = Path.GetFileName(cwd);

        var configuration = options.Configuration ?? "full-build";
        var buildSettings = options.BuildSettings;

        if (!new ConfigurationParser(new FileInfo(cwd)).ConfigurationExists(configuration))
        {
            consoleWriter.WriteError($"Configuration '{configuration}' was not found in {moduleName}.");
            return -1;
        }

        var cleanerLogger = LogManager.GetLogger<Cleaner>();
        var shellRunner = new ShellRunner(LogManager.GetLogger<ShellRunner>());
        var cleaner = new Cleaner(cleanerLogger, shellRunner, consoleWriter);
        var buildYamlScriptsMaker = new BuildYamlScriptsMaker();
        var builder = new ModuleBuilder(consoleWriter, Log, buildSettings, buildYamlScriptsMaker);
        var builderInitTask = Task.Run(() => builder.Init());
        var modulesOrder = buildPreparer.GetModulesOrder(moduleName, configuration);
        var builtStorage = BuildInfoStorage.Deserialize();
        builtStorage.RemoveBuildInfo(moduleName);

        builderInitTask.Wait();
        var module = new Dep(moduleName, null, configuration);

        if (FeatureFlags.CleanBeforeBuild || buildSettings.CleanBeforeBuild)
        {
            if (cleaner.IsNetStandard(module))
                cleaner.Clean(module);
        }

        BuildDepsCommand.TryNugetRestore(Log, consoleWriter, new List<Dep> {module}, builder);

        if (!builder.Build(module))
        {
            builtStorage.Save();
            return -1;
        }

        builtStorage.AddBuiltModule(module, modulesOrder.CurrentCommitHashes);
        builtStorage.Save();
        return 0;
    }
}
