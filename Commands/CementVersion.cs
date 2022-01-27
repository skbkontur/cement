using System.Linq;
using Common;
using Common.Extensions;

namespace Commands
{
    public class CementVersion : ICommand
    {
        public int Run(string[] args)
        {
            var lines = Helper.GetAssemblyTitle().ToLines();
            var version = string.Join("\n", lines.Skip(1).Take(4));
            ConsoleWriter.WriteInfo(version);
            return 0;
        }

        public string HelpMessage => @"
    Shows cement's version

    Usage:
        cm --version
";

        public bool IsHiddenCommand => false;
    }
}