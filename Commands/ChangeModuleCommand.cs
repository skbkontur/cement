﻿using System.Linq;
using Common;
using Common.Exceptions;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class ChangeModuleCommand : Command<ChangeModuleCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "module-change",
        Location = CommandLocation.Any
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly IPackageUpdater packageUpdater;
    private readonly ModuleHelper moduleHelper;

    public ChangeModuleCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IPackageUpdater packageUpdater,
                               ModuleHelper moduleHelper)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.packageUpdater = packageUpdater;
        this.moduleHelper = moduleHelper;
    }

    public override string Name => "add";
    public override string HelpMessage => @"";

    protected override int Execute(ChangeModuleCommandOptions options)
    {
        packageUpdater.UpdatePackages();
        var packages = Helper.GetPackages();

        var packageName = options.PackageName;
        if (packages.Count > 1 && packageName == null)
            throw new CementException($"Specify --package={string.Join("|", packages.Select(p => p.Name))}");

        var package = packageName == null
            ? packages.FirstOrDefault(p => p.Type == "git")
            : packages.FirstOrDefault(p => p.Name == packageName);

        if (package == null)
            throw new CementException("Unable to find " + packageName + " in package list");

        if (package.Type == "git")
            return moduleHelper.ChangeModule(package, options.ModuleName, options.PushUrl, options.FetchUrl);

        consoleWriter.WriteError("You should add local modules file manually");
        return -1;
    }

    protected override ChangeModuleCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseModuleCommand(args);
        var moduleName = (string)parsedArgs["module"];
        var pushUrl = (string)parsedArgs["pushurl"];
        var fetchUrl = (string)parsedArgs["fetchurl"];
        var packageName = (string)parsedArgs["package"];

        return new ChangeModuleCommandOptions(moduleName, pushUrl, fetchUrl, packageName);
    }
}
