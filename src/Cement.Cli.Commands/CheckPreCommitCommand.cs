using System.IO;
using System.Linq;
using System.Text;
using Cement.Cli.Commands.Attributes;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
[HiddenCommand]
public sealed class CheckPreCommitCommand : Command<CheckPreCommitCommandOptions>
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public CheckPreCommitCommand(ConsoleWriter consoleWriter, IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter)
    {
        this.consoleWriter = consoleWriter;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public override CommandLocation Location { get; set; } = CommandLocation.RootModuleDirectory;
    public override string Name => "check-pre-commit";
    public override string HelpMessage => @"
    Checks that commit is good

    Usage:
        cm check-pre-commit
";

    protected override int Execute(CheckPreCommitCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();
        var moduleName = Path.GetFileName(cwd);
        var repo = gitRepositoryFactory.Create(moduleName, Helper.CurrentWorkspace);

        var changedFiles = repo.GetFilesForCommit().Where(file => file.EndsWith(".cs") && File.Exists(file)).Distinct().ToList();
        var exitCode = 0;

        foreach (var file in changedFiles)
        {
            if (!CheckFile(file))
            {
                exitCode = -1;
                consoleWriter.WriteLine("Bad encoding in file: " + file);
            }
        }

        return exitCode;
    }

    protected override CheckPreCommitCommandOptions ParseArgs(string[] args)
    {
        return new CheckPreCommitCommandOptions();
    }

    private static bool CheckFile(string file)
    {
        var bytes = File.ReadAllBytes(file);
        var hasBom = FileHasUtf8Bom(bytes);

        if (hasBom)
            return true;

        return !FileHasNonAsciiSymbols(bytes);
    }

    private static bool FileHasNonAsciiSymbols(byte[] fileBytes)
    {
        return fileBytes.Any(b => b > 127);
    }

    private static bool FileHasUtf8Bom(byte[] fileBytes)
    {
        var preamble = new UTF8Encoding(true).GetPreamble();
        if (fileBytes.Length < preamble.Length)
            return false;
        for (var i = 0; i < preamble.Length; i++)
            if (fileBytes[i] != preamble[i])
                return false;
        return true;
    }
}
