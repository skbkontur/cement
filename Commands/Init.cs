using System.IO;
using Common;

namespace Commands
{
    public class Init : ICommand
    {
        public int Run(string[] args)
        {
            if (args.Length != 1)
            {
                ConsoleWriter.WriteError("Invalid command usage. User 'cm help init' for details");
                return -1;
            }

            var cwd = Directory.GetCurrentDirectory();
            var home = Helper.HomeDirectory();

            if (cwd == home)
            {
                ConsoleWriter.WriteError("$HOME cannot be used as cement base directory");
                return -1;
            }

            if (Helper.IsCementTrackedDirectory(cwd))
            {
                ConsoleWriter.WriteInfo("It is already cement tracked directory");
                return 0;
            }

            Directory.CreateDirectory(Helper.CementDirectory);
            ConsoleWriter.WriteOk(Directory.GetCurrentDirectory() + " became cement tracked directory.");
            return 0;
        }

        public string HelpMessage => @"
    Inits current directory as 'cement tracked'

    Usage:
        cm init

    Note:
        $HOME directory cannot be used with this command
";

        public bool IsHiddenCommand => false;
    }
}