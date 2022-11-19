using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UsagesGrepCommandOptions
{
    public UsagesGrepCommandOptions(string[] arguments, string[] fileMasks, bool skipGet, string checkingBranch)
    {
        Arguments = arguments;
        FileMasks = fileMasks;
        SkipGet = skipGet;
        CheckingBranch = checkingBranch;
    }

    public string[] Arguments { get; }
    public string[] FileMasks { get; }
    public bool SkipGet { get; }
    public string CheckingBranch { get; }
}