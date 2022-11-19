namespace Cement.Cli.Common.Exceptions;

public sealed class GitRemoteException : CementException
{
    public GitRemoteException(string message)
        : base(message)
    {
    }
}
