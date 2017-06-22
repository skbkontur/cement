namespace Commands
{
    public class ReInstall : SelfUpdate
    {
        public ReInstall()
        {
            IsInstallingCement = true;
        }

        public override string HelpMessage => @"
    Reinstalls cement
	NOTE: Don't use this command from installed cement.

    Usage: 
        cm reinstall
";
    }
}