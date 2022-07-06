namespace Common
{
    public sealed class BadArgumentException : CementException
    {
        public BadArgumentException()
        {
        }

        public BadArgumentException(string message)
            : base(message)
        {
        }
    }
}
