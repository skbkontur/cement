namespace Cement.Cli.Common.Exceptions;

public sealed class TreeishConflictException : CementException
{
    public TreeishConflictException(string format)
        : base(format)
    {
    }
}
