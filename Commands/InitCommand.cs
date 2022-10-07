using System.IO;
using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class InitCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;

    public InitCommand(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public string HelpMessage => @"
    Inits current directory as 'cement tracked'

    Usage:
        cm init

    Note:
        $HOME directory cannot be used with this command
";

    public string Name => "init";

    public int Run(string[] args)
    {
        if (args.Length != 1)
        {
            consoleWriter.WriteError("Invalid command usage. User 'cm help init' for details");
            return -1;
        }

        var cwd = Directory.GetCurrentDirectory();
        var home = Helper.HomeDirectory();

        if (cwd == home)
        {
            consoleWriter.WriteError("$HOME cannot be used as cement base directory");
            return -1;
        }

        if (Helper.IsCementTrackedDirectory(cwd))
        {
            consoleWriter.WriteInfo("It is already cement tracked directory");
            return 0;
        }

        Directory.CreateDirectory(Helper.CementDirectory);
        consoleWriter.WriteOk(Directory.GetCurrentDirectory() + " became cement tracked directory.");
        return 0;
    }
}
