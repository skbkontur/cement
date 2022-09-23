using Common;

namespace Commands
{
    public sealed class VersionCommand : ICommand
    {
        private readonly ConsoleWriter consoleWriter;

        public VersionCommand(ConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public string HelpMessage => @"
    Shows cement's version

    Usage:
        cm --version
";

        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            var assemblyTitle = Helper.GetAssemblyTitle();
            consoleWriter.WriteLine(assemblyTitle);
            return 0;
        }
    }
}
