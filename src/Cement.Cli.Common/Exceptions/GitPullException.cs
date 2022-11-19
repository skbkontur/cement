namespace Cement.Cli.Common.Exceptions;

public sealed class GitPullException : CementException
{
    public GitPullException(string message)
        : base(message)
    {
    }
}
