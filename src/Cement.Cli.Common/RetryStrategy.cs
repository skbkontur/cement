namespace Cement.Cli.Common;

public enum RetryStrategy
{
    None,
    IfTimeout,
    IfTimeoutOrFailed
}
