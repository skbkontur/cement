namespace Common
{
    public sealed class GitPullException : CementException
    {
        public GitPullException(string message)
            : base(message)
        {
        }
    }
}
