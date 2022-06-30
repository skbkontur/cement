namespace Common
{
    public sealed class GitRemoteException : CementException
    {
        public GitRemoteException(string message) : base(message)
        {
        }
    }
}