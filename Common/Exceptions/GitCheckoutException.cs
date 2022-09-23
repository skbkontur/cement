namespace Common.Exceptions
{
    public sealed class GitCheckoutException : CementException
    {
        public GitCheckoutException(string message)
            : base(message)
        {
        }
    }
}
