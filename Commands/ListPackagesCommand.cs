﻿#nullable enable
using System.Text;
using Common;

namespace Commands;

public sealed class ListPackagesCommand : Command
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "packages-list",
        MeasureElapsedTime = false,
        Location = CommandLocation.Any
    };
    private readonly ConsoleWriter consoleWriter;

    public ListPackagesCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
    }

    public override string Name => "list";
    public override string HelpMessage => "";

    protected override int Execute()
    {
        var settings = CementSettingsRepository.Get();

        var sb = new StringBuilder();

        foreach (var package in settings.Packages)
            sb.AppendLine($"{package.Name}\t{package.Url}\t{package.Type}");

        consoleWriter.Write(sb.ToString());
        return 0;
    }

    protected override void ParseArgs(string[] args)
    {
    }
}
