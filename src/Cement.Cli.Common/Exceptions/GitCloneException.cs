namespace Cement.Cli.Common.Exceptions;

public sealed class GitCloneException : CementException
{
    public GitCloneException(string message)
        : base(message)
    {
    }
}
