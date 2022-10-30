namespace Common.Exceptions;

public sealed class GitAddException : CementException
{
    public GitAddException(string format)
        : base(format)
    {
    }
}
