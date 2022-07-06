namespace Common
{
    public sealed class GitBranchException : CementException
    {
        public GitBranchException(string message)
            : base(message)
        {
        }
    }
}
