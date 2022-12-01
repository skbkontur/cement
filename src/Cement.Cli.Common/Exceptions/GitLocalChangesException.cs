namespace Cement.Cli.Common.Exceptions;

public sealed class GitLocalChangesException : CementException
{
    public GitLocalChangesException(string s)
        : base(s)
    {
    }
}
