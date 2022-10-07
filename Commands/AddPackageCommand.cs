#nullable enable
using System;
using Common;
using Common.Exceptions;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class AddPackageCommand : Command<AddPackageCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "package-add",
        Location = CommandLocation.Any
    };

    public AddPackageCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
    }

    public override string Name => "add";
    public override string HelpMessage => @"usage: cm packages add <name> <url>";

    protected override int Execute(AddPackageCommandOptions options)
    {
        var settings = CementSettingsRepository.Get();
        var packageName = options.PackageName;
        var packageUrl = options.PackageUrl;

        var package = settings.Packages.Find(p => p.Name.Equals(packageName, StringComparison.Ordinal));
        if (package != null)
        {
            if (package.Url.Equals(packageUrl))
                return 0;

            throw new CementException($"error: conflict: package '{packageName}' already exists");
        }

        package = new Package(packageName, packageUrl);
        settings.Packages.Add(package);

        CementSettingsRepository.Save(settings);
        return 0;
    }

    protected override AddPackageCommandOptions ParseArgs(string[] args)
    {
        if (args.Length < 2)
            throw new BadArgumentException($"error: invalid arguments{Environment.NewLine}{HelpMessage}");

        var packageName = args[0];
        var packageUrl = args[1];

        return new AddPackageCommandOptions(packageName, packageUrl);
    }
}
