using Common;

namespace Commands
{
    public sealed class VersionCommand : ICommand
    {
        public string HelpMessage => @"
    Shows cement's version

    Usage:
        cm --version
";

        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            var assemblyTitle = Helper.GetAssemblyTitle();
            ConsoleWriter.Shared.WriteLine(assemblyTitle);
            return 0;
        }
    }
}
