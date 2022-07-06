namespace Common
{
    public sealed class GitPushException : CementException
    {
        public GitPushException(string s)
            : base(s)
        {
        }
    }
}
