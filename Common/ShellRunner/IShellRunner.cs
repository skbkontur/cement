using System;

namespace Common
{
    // TODO (DonMorozov): пробежаться по всему проекту с целью поменять интерфейс для более красивого кода
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
    
    // TODO (DonMorozov): избавиться от класса, потребуется рефакторинг UsagesBuild
    public static class ShellRunnerStaticInfo
    {
        public static string LastOutput { get; set; }
    }
    
    public delegate void ReadLineEvent(string content);
    
    // TODO (DonMorozov): реализация выбора стратегии через enum - не самый красивый путь, для более красивого решения потребуются относительно масштабные изменения
    public enum RetryStrategy
    {
        None,
        IfTimeout,
        IfTimeoutOrFailed
    }
}