namespace Common.Exceptions
{
    public sealed class CementBuildException : CementException
    {
        public CementBuildException(string message)
            : base(message)
        {
        }
    }
}
