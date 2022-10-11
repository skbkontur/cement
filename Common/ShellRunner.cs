using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security;
using System.Text;
using System.Threading;
using Common.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using TimeoutException = Common.Exceptions.TimeoutException;

namespace Common;

[PublicAPI]
public sealed class ShellRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
    private readonly ProcessStartInfo startInfo;
    private readonly ILogger logger;

    public event ReadLineEvent OnOutputChange;
    public event ReadLineEvent OnErrorsChange;

    public ShellRunner(ILogger<ShellRunner> logger)
    {
        this.logger = logger;

        startInfo = new ProcessStartInfo
        {
            FileName = Platform.IsUnix() ? "/bin/bash" : "cmd",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        };
        AddUserPassword();
    }

    public static string LastOutput { get; private set; }

    public ShellRunnerResult RunOnce(string commandWithArguments, string workingDirectory,
                                     TimeSpan timeout)
    {
        startInfo.Arguments = Platform.IsUnix() ? " -lc " : " /D /C ";
        var hasTimeout = false;

        const char quote = '"';
        startInfo.Arguments = startInfo.Arguments + quote + commandWithArguments + quote;
        startInfo.WorkingDirectory = workingDirectory;

        var output = string.Empty;
        var errors = string.Empty;

        var sw = Stopwatch.StartNew();
        using var process = Process.Start(startInfo);
        try
        {
            var threadOutput = new Thread(() => ReadStream(process.StandardOutput, OnOutputChange));
            var threadErrors = new Thread(() => ReadStream(process.StandardError, OnErrorsChange));

            threadOutput.Start();
            threadErrors.Start();

            if (!threadOutput.Join(timeout) || !threadErrors.Join(timeout) || !process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                threadOutput.Join();
                threadErrors.Join();

                threadOutput.Interrupt();
                threadErrors.Interrupt();

                threadOutput.Join();
                threadErrors.Join();

                KillProcessAndChildren(process.Id, new HashSet<int>());

                hasTimeout = true;

                var message = string.Format("Running timeout at {2} for command {0} in {1}", commandWithArguments, workingDirectory, timeout);
                errors += message;
                throw new TimeoutException(message);
            }

            LastOutput = output;
            var exitCode = process.ExitCode;

            logger.LogInformation(
                $"EXECUTED {startInfo.FileName} {startInfo.Arguments} in {workingDirectory} " +
                $"in {sw.ElapsedMilliseconds}ms with exitCode {exitCode}");

            return new ShellRunnerResult(exitCode, output, errors, hasTimeout);
        }
        catch (CementException e)
        {
            if (e is TimeoutException)
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

    private void AddUserPassword()
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

    private string ReadStream(StreamReader output, ReadLineEvent evt)
    {
        var result = new StringBuilder();
        while (!output.EndOfStream)
        {
            var line = output.ReadLine();
            evt?.Invoke(line);
            result.Append(line + "\n");
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
                $"EXECUTED {startInfo.FileName} {startInfo.Arguments} in {workingDirectory} " +
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

    private void KillProcessAndChildren(int pid, HashSet<int> killed)
    {
        if (killed.Contains(pid))
            return;
        killed.Add(pid);

        var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
        var moc = searcher.Get();
        foreach (var mo in moc)
        {
            var child = Convert.ToInt32(mo["ProcessID"]);
            KillProcessAndChildren(child, killed);
        }

        try
        {
            var proc = Process.GetProcessById(pid);
            if (!IsCementProcess(proc.ProcessName))
                return;

            logger.LogDebug("kill " + proc.ProcessName + "#" + proc.Id);
            proc.Kill();
        }
        catch (Exception exception)
        {
            logger.LogDebug("killing already exited process #" + pid, exception);
        }
    }

    public delegate void ReadLineEvent(string content);
}
