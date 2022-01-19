using System;

namespace Common
{
    public interface IShellRunner
    {
        string Output { get; }
        string Errors { get; }
        bool HasTimeout { get; }
        
        event ReadLineEvent OnOutputChange, OnErrorsChange;

        int RunOnce(string commandWithArguments, string workingDirectory, TimeSpan timeout);
        int Run(string commandWithArguments);
        int Run(string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout);
        int RunInDirectory(string path, string commandWithArguments);
        int RunInDirectory(string path, string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout);
    }
    
    public static class ShellRunnerStaticInfo
    {
        public static string LastOutput { get; set; }
    }
    
    public delegate void ReadLineEvent(string content);
    
    public enum RetryStrategy
    {
        None,
        IfTimeout,
        IfTimeoutOrFailed
    }
}