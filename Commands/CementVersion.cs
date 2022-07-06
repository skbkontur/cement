using System.Linq;
using Common;

namespace Commands
{
    public class CementVersion : ICommand
    {
        public string HelpMessage => @"
    Shows cement's version

    Usage:
        cm --version
";

        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            var lines = Helper.GetAssemblyTitle().Split('\n');
            var version = string.Join("\n", lines.Skip(1).Take(4));
            ConsoleWriter.Shared.WriteInfo(version);
            return 0;
        }
    }
}
