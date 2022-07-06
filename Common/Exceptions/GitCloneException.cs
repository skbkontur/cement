namespace Common
{
    public sealed class GitCloneException : CementException
    {
        public GitCloneException(string message)
            : base(message)
        {
        }
    }
}
