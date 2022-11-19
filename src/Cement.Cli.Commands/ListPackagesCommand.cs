#nullable enable
using System.Text;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class ListPackagesCommand : Command<ListPackagesCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
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

    protected override int Execute(ListPackagesCommandOptions options)
    {
        var settings = CementSettingsRepository.Get();

        var sb = new StringBuilder();

        foreach (var package in settings.Packages)
            sb.AppendLine($"{package.Name}\t{package.Url}\t{package.Type}");

        consoleWriter.Write(sb.ToString());
        return 0;
    }

    protected override ListPackagesCommandOptions ParseArgs(string[] args)
    {
        return new ListPackagesCommandOptions();
    }
}
