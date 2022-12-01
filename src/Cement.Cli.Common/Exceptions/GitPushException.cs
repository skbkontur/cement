namespace Cement.Cli.Common.Exceptions;

public sealed class GitPushException : CementException
{
    public GitPushException(string s)
        : base(s)
    {
    }
}
