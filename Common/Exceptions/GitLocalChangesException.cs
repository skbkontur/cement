namespace Common
{
    public sealed class GitLocalChangesException : CementException
    {
        public GitLocalChangesException(string s)
            : base(s)
        {
        }
    }
}
