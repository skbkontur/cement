using Common;

namespace Commands
{
    public sealed class ReInstallCommand : SelfUpdateCommand
    {
        public ReInstallCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, featureFlags)
        {
            IsInstallingCement = true;
            CommandSettings.IsHiddenCommand = true;
        }

        public override string HelpMessage => @"
    Reinstalls cement
	NOTE: Don't use this command from installed cement.

    Usage:
        cm reinstall
";
    }
}
