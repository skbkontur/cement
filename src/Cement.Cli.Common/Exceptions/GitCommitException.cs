namespace Cement.Cli.Common.Exceptions;

public sealed class GitCommitException : CementException
{
    public GitCommitException(string s)
        : base(s)
    {
    }
}
