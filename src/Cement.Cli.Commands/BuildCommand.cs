using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class BuildCommand : Command<BuildCommandOptions>
{
    private readonly ILogger<BuildCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly FeatureFlags featureFlags;
    private readonly BuildPreparer buildPreparer;

    public BuildCommand(ILogger<BuildCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags,
                        BuildPreparer buildPreparer)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.featureFlags = featureFlags;
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
        return new BuildCommandOptionsParser().Parse(args);
    }

    protected override int Execute(BuildCommandOptions options)
    {
        CommandHelper.SetWorkspace(CommandLocation.RootModuleDirectory);

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
        var builder = new ModuleBuilder(logger, consoleWriter, buildSettings, buildYamlScriptsMaker);
        var builderInitTask = Task.Run(() => builder.Init());
        var modulesOrder = buildPreparer.GetModulesOrder(moduleName, configuration);
        var builtStorage = BuildInfoStorage.Deserialize();
        builtStorage.RemoveBuildInfo(moduleName);

        builderInitTask.Wait();
        var module = new Dep(moduleName, null, configuration);

        if (featureFlags.CleanBeforeBuild || buildSettings.CleanBeforeBuild)
        {
            if (cleaner.IsNetStandard(module))
                cleaner.Clean(module);
        }

        BuildDepsCommand.TryNugetRestore(logger, consoleWriter, new List<Dep> {module}, builder);

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
