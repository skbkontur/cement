using System;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class CliWrapProxy: IShellRunner
    {
        public string Output { get; private set; }
        public string Errors { get; private set; }
        public bool HasTimeout { get; private set; }
        public event ReadLineEvent OnOutputChange
        {
            add
            {
                cliWrapRunner.OnOutputChange += value;
                shellRunner.OnOutputChange += value;
            }
            remove
            {
                cliWrapRunner.OnOutputChange -= value;
                shellRunner.OnOutputChange -= value;
            }
        }
        public event ReadLineEvent OnErrorsChange
        {
            add
            {
                cliWrapRunner.OnErrorsChange += value;
                shellRunner.OnErrorsChange += value;
            }
            remove
            {
                cliWrapRunner.OnErrorsChange -= value;
                shellRunner.OnErrorsChange -= value;
            }
        }

        private CliWrapRunner cliWrapRunner;
        private ShellRunner shellRunner;

        public CliWrapProxy(ILogger logger)
        {
            cliWrapRunner = new CliWrapRunner(logger);
            shellRunner = new ShellRunner(logger);
        }

        public int RunOnce(string commandWithArguments, string workingDirectory, TimeSpan timeout)
        {
            try
            {
                var result = cliWrapRunner.RunOnce(commandWithArguments, workingDirectory, timeout);
                Output = cliWrapRunner.Output;
                Errors = cliWrapRunner.Errors;
                HasTimeout = cliWrapRunner.HasTimeout;
                return result;
            }
            catch (CementException)
            {
                throw;
            }
            catch (Exception)
            {
                var result = shellRunner.RunOnce(commandWithArguments, workingDirectory, timeout);
                Output = shellRunner.Output;
                Errors = shellRunner.Errors;
                HasTimeout = shellRunner.HasTimeout;
                return result;
            }
        }

        public int Run(string commandWithArguments)
        {
            try
            {
                var result = cliWrapRunner.Run(commandWithArguments);
                Output = cliWrapRunner.Output;
                Errors = cliWrapRunner.Errors;
                HasTimeout = cliWrapRunner.HasTimeout;
                return result;
            }
            catch (CementException)
            {
                throw;
            }
            catch (Exception)
            {
                var result = shellRunner.Run(commandWithArguments);
                Output = shellRunner.Output;
                Errors = shellRunner.Errors;
                HasTimeout = shellRunner.HasTimeout;
                return result;
            }
        }

        public int Run(string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            try
            {
                var result = cliWrapRunner.Run(commandWithArguments, timeout, retryStrategy);
                Output = cliWrapRunner.Output;
                Errors = cliWrapRunner.Errors;
                HasTimeout = cliWrapRunner.HasTimeout;
                return result;
            }
            catch (CementException)
            {
                throw;
            }
            catch (Exception)
            {
                var result = shellRunner.Run(commandWithArguments, timeout, retryStrategy);
                Output = shellRunner.Output;
                Errors = shellRunner.Errors;
                HasTimeout = shellRunner.HasTimeout;
                return result;
            }
        }

        public int RunInDirectory(string path, string commandWithArguments)
        {
            try
            {
                var result = cliWrapRunner.RunInDirectory(path, commandWithArguments);
                Output = cliWrapRunner.Output;
                Errors = cliWrapRunner.Errors;
                HasTimeout = cliWrapRunner.HasTimeout;
                return result;
            }
            catch (CementException)
            {
                throw;
            }
            catch (Exception)
            {
                var result = shellRunner.RunInDirectory(path, commandWithArguments);
                Output = shellRunner.Output;
                Errors = shellRunner.Errors;
                HasTimeout = shellRunner.HasTimeout;
                return result;
            }
        }

        public int RunInDirectory(string path, string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            try
            {
                var result = cliWrapRunner.RunInDirectory(path, commandWithArguments, timeout, retryStrategy);
                Output = cliWrapRunner.Output;
                Errors = cliWrapRunner.Errors;
                HasTimeout = cliWrapRunner.HasTimeout;
                return result;
            }
            catch (CementException)
            {
                throw;
            }
            catch (Exception)
            {
                var result = shellRunner.RunInDirectory(path, commandWithArguments, timeout, retryStrategy);
                Output = shellRunner.Output;
                Errors = shellRunner.Errors;
                HasTimeout = shellRunner.HasTimeout;
                return result;
            }
        }
    }
}