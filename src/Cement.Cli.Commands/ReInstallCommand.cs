using Cement.Cli.Commands.Attributes;
using Cement.Cli.Commands.Extensions;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
[HiddenCommand]
public sealed class ReInstallCommand : Command<ReInstallCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        Location = CommandLocation.Any
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly FeatureFlags featureFlags;
    private readonly ICommandActivator commandActivator;

    public ReInstallCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, ICommandActivator commandActivator)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.featureFlags = featureFlags;
        this.commandActivator = commandActivator;
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
        var selfUpdateCommand = commandActivator.Create<SelfUpdateCommand>();
        selfUpdateCommand.IsInstallingCement = true;

        return selfUpdateCommand.Run(new[] {"self-update"});
    }

    protected override ReInstallCommandOptions ParseArgs(string[] args)
    {
        return new ReInstallCommandOptions();
    }
}
