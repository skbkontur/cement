using System.Linq;
using Common;

namespace Commands
{
	public class CementVersion : ICommand
    {
        public int Run(string[] args)
        {
            var lines = Helper.GetAssemblyTitle().Split('\n');
            var version = string.Join("\n", lines.Take(4));
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
