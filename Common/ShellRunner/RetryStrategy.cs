namespace Common
{
    public enum RetryStrategy
    {
        None,
        IfTimeout,
        IfTimeoutOrFailed
    }
}