using System.IO;
using Common;

namespace Commands
{
    public class Init : ICommand
    {
        public string HelpMessage => @"
    Inits current directory as 'cement tracked'

    Usage:
        cm init

    Note:
        $HOME directory cannot be used with this command
";

        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            if (args.Length != 1)
            {
                ConsoleWriter.Shared.WriteError("Invalid command usage. User 'cm help init' for details");
                return -1;
            }

            var cwd = Directory.GetCurrentDirectory();
            var home = Helper.HomeDirectory();

            if (cwd == home)
            {
                ConsoleWriter.Shared.WriteError("$HOME cannot be used as cement base directory");
                return -1;
            }

            if (Helper.IsCementTrackedDirectory(cwd))
            {
                ConsoleWriter.Shared.WriteInfo("It is already cement tracked directory");
                return 0;
            }

            Directory.CreateDirectory(Helper.CementDirectory);
            ConsoleWriter.Shared.WriteOk(Directory.GetCurrentDirectory() + " became cement tracked directory.");
            return 0;
        }
    }
}
