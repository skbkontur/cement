using Commands.Attributes;
using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
[HiddenCommand]
public sealed class ReInstallCommand : Command<ReInstallCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "reinstall",
        Location = CommandLocation.Any
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly FeatureFlags featureFlags;

    public ReInstallCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.featureFlags = featureFlags;
    }

    public override string Name => "reinstall";
    public override string HelpMessage => @"
    Reinstalls cement
	NOTE: Don't use this command from installed cement.

    Usage:
        cm reinstall
";

    protected override int Execute(ReInstallCommandOptions options)
    {
        var selfUpdateCommand = new SelfUpdateCommand(consoleWriter, featureFlags)
        {
            IsInstallingCement = true
        };

        return selfUpdateCommand.Run(new[] {"self-update"});
    }

    protected override ReInstallCommandOptions ParseArgs(string[] args)
    {
        return new ReInstallCommandOptions();
    }
}
