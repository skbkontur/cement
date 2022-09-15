#nullable enable
using System;
using Common;

namespace Commands;

public sealed class RemovePackageCommand : Command
{
    private static readonly CommandSettings Settings = new()
    {
        LogPerfix = "PACKAGE-REMOVE",
        LogFileName = "package-remove",
        MeasureElapsedTime = false,
        Location = CommandSettings.CommandLocation.Any
    };

    private string packageName = null!;

    public RemovePackageCommand()
        : base(Settings)
    {
    }

    public override string HelpMessage => @"usage: cm packages remove <name>";

    protected override int Execute()
    {
        var settings = CementSettingsRepository.Get();

        var package = settings.Packages.Find(p => p.Name.Equals(packageName, StringComparison.Ordinal));
        if (package == null)
            return 0;

        settings.Packages.Remove(package);

        CementSettingsRepository.Save(settings);
        return 0;
    }

    protected override void ParseArgs(string[] args)
    {
        if (args.Length < 1)
            throw new BadArgumentException($"error: invalid arguments{Environment.NewLine}{HelpMessage}");

        packageName = args[0];
    }
}
