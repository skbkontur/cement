using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class AddModuleCommand : Command<AddModuleCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        Location = CommandLocation.Any
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly IPackageUpdater packageUpdater;
    private readonly ModuleHelper moduleHelper;

    public AddModuleCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IPackageUpdater packageUpdater,
                            ModuleHelper moduleHelper)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.packageUpdater = packageUpdater;
        this.moduleHelper = moduleHelper;
    }

    public override string Name => "add";
    public override string HelpMessage => @"";

    protected override int Execute(AddModuleCommandOptions options)
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
            return moduleHelper.AddModule(package, options.ModuleName, options.PushUrl, options.FetchUrl);

        consoleWriter.WriteError("You should add local modules file manually");
        return -1;
    }

    protected override AddModuleCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseModuleCommand(args);
        var moduleName = (string)parsedArgs["module"];
        var pushUrl = (string)parsedArgs["pushurl"];
        var fetchUrl = (string)parsedArgs["fetchurl"];
        var packageName = (string)parsedArgs["package"];

        return new AddModuleCommandOptions(moduleName, pushUrl, fetchUrl, packageName);
    }
}
