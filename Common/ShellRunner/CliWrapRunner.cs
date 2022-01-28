using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class CliWrapRunner: IShellRunner
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

        private readonly string targetPath;
        private readonly string argumentsPrefix;
        
        public string Output { get; private set; }
        public string Errors { get; private set; }
        public bool HasTimeout { get; private set; }
        
        public event ReadLineEvent OnOutputChange;
        public event ReadLineEvent OnErrorsChange;

        private readonly ILogger logger;

        private readonly string domain;
        private readonly string userName;
        private readonly string password; 

        public CliWrapRunner(ILogger logger = null)
        {
            this.logger = logger ?? LogManager.GetLogger(typeof(ModuleGetter));

            targetPath = Helper.OsIsUnix() ? "/bin/bash" : "cmd";
            argumentsPrefix = Helper.OsIsUnix() ? " -lc " : " /D /C ";
            
            var settings = CementSettings.Get();
            if (settings.UserName == null || settings.EncryptedPassword == null)
                return;

            domain = settings.Domain ?? Environment.MachineName;
            userName = settings.UserName;
            password = Helper.Decrypt(settings.EncryptedPassword);
        }

        public int RunOnce(string commandWithArguments, string workingDirectory, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);

            var command = Cli.Wrap(targetPath)
                .WithArguments($"{argumentsPrefix} {commandWithArguments}")
                .WithWorkingDirectory(workingDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStream(new ReadLineEventStream(OnOutputChange)))
                .WithStandardErrorPipe(PipeTarget.ToStream(new ReadLineEventStream(OnErrorsChange)))
                .WithValidation(CommandResultValidation.None);
            if (!string.IsNullOrWhiteSpace(userName))
                command = command.WithCredentials(builder => builder.SetDomain(domain).SetUserName(userName).SetPassword(password).Build());
            var task = command.ExecuteBufferedAsync(cancellationTokenSource.Token);

            var result = (BufferedCommandResult)null;
            
            try
            {
                HasTimeout = false;
                Output = "";
                Errors = "";
                
                result = Task.Run(async() => await task).Result;
            }
            catch (AggregateException exception)
            {
                switch (exception.InnerException)
                {
                    case null:
                        throw;
                    
                    case TaskCanceledException _:
                    case TimeoutException _:
                        if (!commandWithArguments.Equals("git ls-remote --heads"))
                            ConsoleWriter.WriteWarning(exception.InnerException?.Message);
                        logger?.LogWarning(exception.Message);
                        HasTimeout = true;
                        Errors += $"Running timeout at {timeout} for command {commandWithArguments} in {workingDirectory}";
                        return -1;
                    
                    case CementException _:
                        ConsoleWriter.WriteError(exception.InnerException?.Message);
                        logger.LogError(exception.InnerException?.Message);
                        return -1;
                    
                    default:
                        throw exception.InnerException;
                }
            }
            
            Output = result?.StandardOutput ?? "";
            Errors = result?.StandardError ?? "";
            ShellRunnerStaticInfo.LastOutput = Output;
            
            logger.LogInformation($"EXECUTED {commandWithArguments} in {workingDirectory} in {result.RunTime.TotalMilliseconds}ms with exitCode {result.ExitCode}");
            return result.ExitCode;
        }

        public int Run(string commandWithArguments) =>
            Run(commandWithArguments, DefaultTimeout);

        public int Run(string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout) =>
            RunThreeTimes(commandWithArguments, Directory.GetCurrentDirectory(), timeout, retryStrategy);

        public int RunInDirectory(string path, string commandWithArguments) =>
            RunInDirectory(path, commandWithArguments, DefaultTimeout);

        public int RunInDirectory(string path, string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout) =>
            RunThreeTimes(commandWithArguments, path, timeout, retryStrategy);

        private int RunThreeTimes(string commandWithArguments, string workingDirectory, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            int exitCode = RunOnce(commandWithArguments, workingDirectory, timeout);
            int times = 2;

            while (times-- > 0 && NeedRunAgain(retryStrategy, exitCode))
            {
                if (HasTimeout)
                    timeout = TimeoutHelper.IncreaseTimeout(timeout);
                exitCode = RunOnce(commandWithArguments, workingDirectory, timeout);
                logger.LogDebug($"EXECUTED {commandWithArguments} in {workingDirectory} with exitCode {exitCode} and retryStrategy {retryStrategy}");
            }
            return exitCode;
        }
        
        private bool NeedRunAgain(RetryStrategy retryStrategy, int exitCode)
        {
            if (retryStrategy == RetryStrategy.IfTimeout && HasTimeout)
                return true;
            if (retryStrategy == RetryStrategy.IfTimeoutOrFailed && (exitCode != 0 || HasTimeout))
                return true;
            return false;
        }
    }
}