using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
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

    public string Name => "--version";

    public int Run(string[] args)
    {
        var assemblyTitle = Helper.GetAssemblyTitle();
        consoleWriter.WriteLine(assemblyTitle);
        return 0;
    }
}
