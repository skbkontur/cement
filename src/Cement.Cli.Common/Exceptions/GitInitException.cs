namespace Cement.Cli.Common.Exceptions;

public sealed class GitInitException : CementException
{
    public GitInitException(string format)
        : base(format)
    {
    }
}
