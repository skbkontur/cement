namespace Commands
{
    public sealed class ReInstallCommand : SelfUpdateCommand
    {
        public ReInstallCommand()
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
