namespace Common
{
    public sealed class TimeoutException : CementException
    {
        public TimeoutException(string format)
            : base(format)
        {
        }
    }
}
