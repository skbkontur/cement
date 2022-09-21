#nullable enable
using System;
using Common;

namespace Commands;

public sealed class AddPackageCommand : Command
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "package-add",
        MeasureElapsedTime = false,
        Location = CommandSettings.CommandLocation.Any
    };

    private string packageName = null!;
    private string packageUrl = null!;

    public AddPackageCommand()
        : base(Settings)
    {
    }

    public override string HelpMessage => @"usage: cm packages add <name> <url>";

    protected override int Execute()
    {
        var settings = CementSettingsRepository.Get();

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

    protected override void ParseArgs(string[] args)
    {
        if (args.Length < 2)
            throw new BadArgumentException($"error: invalid arguments{Environment.NewLine}{HelpMessage}");

        packageName = args[0];
        packageUrl = args[1];
    }
}
