namespace Common
{
    public sealed class GitInitException : CementException
    {
        public GitInitException(string format)
            : base(format)
        {
        }
    }
}
