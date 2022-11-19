using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using TimeoutException = Cement.Cli.Common.Exceptions.TimeoutException;

namespace Cement.Cli.Common;

[PublicAPI]
public sealed class ShellRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
    private readonly ILogger logger;

    public event ReadLineEvent OnOutputChange;
    public event ReadLineEvent OnErrorsChange;

    public ShellRunner(ILogger<ShellRunner> logger)
    {
        this.logger = logger;
    }

    private ProcessStartInfo GetStartInfo(string fileName, string arguments, string workingDir)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        };
    }

    public static string LastOutput { get; private set; }

    public ShellRunnerResult RunOnce(string commandWithArguments, string workingDirectory,
                                     TimeSpan timeout)
    {
        const char quote = '"';

        var fileName = Platform.IsUnix() ? "/bin/bash" : "cmd";
        var arguments = (Platform.IsUnix() ? " -lc " : " /D /C ") + quote + commandWithArguments + quote;

        var startInfo = GetStartInfo(fileName, arguments, workingDirectory);
        if (OperatingSystem.IsWindows())
            AddUserPassword(startInfo);

        var hasTimeout = false;
        var output = string.Empty;
        var errors = string.Empty;

        try
        {
            using var process = new ProcessEx(startInfo);
            process.Start();

            var threadOutput = new Thread(() => output = ReadStream(process.StandardOutput, OnOutputChange));
            var threadErrors = new Thread(() => errors = ReadStream(process.StandardError, OnErrorsChange));

            threadOutput.Start();
            threadErrors.Start();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds) || !threadOutput.Join(timeout) || !threadErrors.Join(timeout))
            {
                threadOutput.Join();
                threadErrors.Join();

                threadOutput.Interrupt();
                threadErrors.Interrupt();

                threadOutput.Join();
                threadErrors.Join();

                process.Kill();
                hasTimeout = true;

                throw new Exceptions.TimeoutException($"Running timeout at {timeout} for command {commandWithArguments} in {workingDirectory}");
            }

            LastOutput = output;

            var exitCode = process.ExitCode;
            var runTime = process.ExitTime - process.StartTime;

            logger.LogInformation(
                $"EXECUTED {startInfo.FileName} {startInfo.Arguments} in {workingDirectory} " +
                $"in {runTime:c} with exitCode {exitCode}");

            return new ShellRunnerResult(exitCode, output, errors, hasTimeout);
        }
        catch (CementException e)
        {
            if (e is System.TimeoutException)
            {
                if (!commandWithArguments.Equals("git ls-remote --heads"))
                    ConsoleWriter.Shared.WriteWarning(e.Message);
                logger.LogWarning(e.Message);
            }
            else
            {
                ConsoleWriter.Shared.WriteError(e.Message);
                logger.LogError(e.Message);
            }

            return new ShellRunnerResult(-1, output, errors, hasTimeout);
        }
    }

    public ShellRunnerResult Run(string commandWithArguments)
    {
        return Run(commandWithArguments, DefaultTimeout);
    }

    public ShellRunnerResult Run(string commandWithArguments, TimeSpan timeout,
                                 RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
    {
        return RunThreeTimes(commandWithArguments, Directory.GetCurrentDirectory(), timeout, retryStrategy);
    }

    public ShellRunnerResult RunInDirectory(string path, string commandWithArguments)
    {
        return RunInDirectory(path, commandWithArguments, DefaultTimeout);
    }

    public ShellRunnerResult RunInDirectory(string path, string commandWithArguments, TimeSpan timeout,
                                            RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
    {
        return RunThreeTimes(commandWithArguments, path, timeout, retryStrategy);
    }

    private static bool IsCementProcess(string process)
    {
        return process == "cmd" || process.StartsWith("ssh") || process.StartsWith("git");
    }

    [SupportedOSPlatform("windows")]
    private void AddUserPassword(ProcessStartInfo startInfo)
    {
        var settings = CementSettingsRepository.Get();
        if (settings.UserName == null || settings.EncryptedPassword == null)
            return;

        startInfo.Domain = settings.Domain ?? Environment.MachineName;
        startInfo.UserName = settings.UserName;
        var decryptedPassword = Helper.Decrypt(settings.EncryptedPassword);

        var password = new SecureString();
        foreach (var c in decryptedPassword)
            password.AppendChar(c);

        startInfo.Password = password;
    }

    private static string ReadStream(StreamReader streamReader, ReadLineEvent evt)
    {
        var result = new StringBuilder();

        while (!streamReader.EndOfStream)
        {
            var line = streamReader.ReadLine();

            result.AppendLine(line);
            evt?.Invoke(line);
        }

        return result.ToString();
    }

    private ShellRunnerResult RunThreeTimes(string commandWithArguments, string workingDirectory,
                                            TimeSpan timeout,
                                            RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
    {
        var (exitCode, output, errors, hasTimeout) = RunOnce(commandWithArguments, workingDirectory, timeout);
        var times = 2;

        while (times-- > 0 && NeedRunAgain(retryStrategy, exitCode, hasTimeout))
        {
            if (hasTimeout)
                timeout = TimeoutHelper.IncreaseTimeout(timeout);

            (exitCode, output, errors, hasTimeout) = RunOnce(commandWithArguments, workingDirectory, timeout);

            logger.LogDebug(
                $"EXECUTED {commandWithArguments} in {workingDirectory} " +
                $"with exitCode {exitCode} and retryStrategy {retryStrategy}");
        }

        return new ShellRunnerResult(exitCode, output, errors, hasTimeout);
    }

    private bool NeedRunAgain(RetryStrategy retryStrategy, int exitCode, bool hasTimeout)
    {
        if (retryStrategy == RetryStrategy.IfTimeout && hasTimeout)
            return true;
        if (retryStrategy == RetryStrategy.IfTimeoutOrFailed && (exitCode != 0 || hasTimeout))
            return true;
        return false;
    }

    public delegate void ReadLineEvent(string content);
}
