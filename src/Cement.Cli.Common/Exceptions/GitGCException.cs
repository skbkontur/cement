namespace Cement.Cli.Common.Exceptions;

public sealed class GitGCException : CementException
{
    public GitGCException(string message)
        : base(message)
    {
    }
}
