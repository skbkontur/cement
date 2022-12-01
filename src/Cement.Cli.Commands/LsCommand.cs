using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.YamlParsers;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class LsCommand : Command<LsCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        RequireModuleYaml = false,
        Location = CommandLocation.Any
    };

    private readonly ConsoleWriter consoleWriter;
    private readonly IPackageUpdater packageUpdater;

    public LsCommand(ConsoleWriter consoleWriter, IPackageUpdater packageUpdater, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.packageUpdater = packageUpdater;
    }

    public override string Name => "ls";
    public override string HelpMessage => @"
    Lists all available modules

    Usage:
        cm ls [-l|-a] [-b=<branch>] [-u] [-p]

        -l/--local                   lists modules in current directory
        -a/--all                     lists all cement-known modules (default)

        -b/--has-branch<=branch>     lists all modules which have such branch
                                     --local key by default

        -u/--url                     shows module fetch url
        -p/--pushurl                 shows module pushurl

    Example:
        cm ls --all --has-branch=temp --url
";

    protected override LsCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseLs(args);

        return new LsCommandOptions(
            (bool)parsedArgs["simple"],
            GetModuleProcessType(parsedArgs),
            (bool)parsedArgs["url"],
            (bool)parsedArgs["pushurl"],
            (string)parsedArgs["branch"]);
    }

    protected override int Execute(LsCommandOptions commandOptions)
    {
        if (commandOptions.IsSimpleMode)
        {
            PrintSimpleLocalWithYaml();
            return 0;
        }

        packageUpdater.UpdatePackages();
        var packages = Helper.GetPackages();
        foreach (var package in packages)
            PrintPackage(package, commandOptions);

        consoleWriter.ClearLine();
        return 0;
    }

    private void PrintSimpleLocalWithYaml()
    {
        var modules = Helper.GetModules();
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
        Helper.SetWorkspace(workspace);
        var local = modules.Where(m => Yaml.Exists(m.Name));
        consoleWriter.SimpleWriteLine(string.Join("\n", local.Select(m => m.Name).OrderBy(x => x)));
    }

    private void PrintPackage(Package package, LsCommandOptions options)
    {
        consoleWriter.SimpleWriteLine("[{0}]", package.Name);
        var modules = Helper.GetModulesFromPackage(package).OrderBy(m => m.Name);
        foreach (var module in modules)
            PrintModule(module, options);
        consoleWriter.ClearLine();
    }

    private void PrintModule(Module module, LsCommandOptions options)
    {
        consoleWriter.WriteProgress(module.Name);
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();

        if (options.ModuleProcessType == ModuleProcessType.Local &&
            !Helper.DirectoryContainsModule(workspace, module.Name))
            return;
        if (options.BranchName != null && !GitRepository.HasRemoteBranch(module.Url, options.BranchName))
            return;

        consoleWriter.ClearLine();
        consoleWriter.SimpleWrite("{0, -30}", module.Name);
        if (options.ShowUrl)
            consoleWriter.SimpleWrite("{0, -60}", module.Url);
        if (options.ShowPushUrl)
            consoleWriter.SimpleWrite(module.Url);
        consoleWriter.SimpleWriteLine();
    }

    private static ModuleProcessType GetModuleProcessType(Dictionary<string, object> parsedArgs)
    {
        var (isLocal, isAllModules) = ((bool)parsedArgs["local"], (bool)parsedArgs["all"]);
        if (isLocal)
            return ModuleProcessType.Local;

        if (isAllModules)
            return ModuleProcessType.All;

        return (string)parsedArgs["branch"] is null
            ? ModuleProcessType.All
            : ModuleProcessType.Local;
    }
}
