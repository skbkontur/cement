using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security;
using System.Text;
using System.Threading;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class ShellRunner
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        public static string LastOutput;
        public string Output { get; private set; }
        public string Errors { get; private set; }
        public bool HasTimeout;

        private readonly ProcessStartInfo startInfo;
        private readonly ILogger log;

        public ShellRunner(ILogger log = null)
        {
            if (log == null)
                log = LogManager.GetLogger(typeof(ModuleGetter));

            this.log = log;
            startInfo = new ProcessStartInfo
            {
                FileName = Helper.OsIsUnix() ? "/bin/bash" : "cmd",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };
            AddUserPassword();
        }

        private void AddUserPassword()
        {
            var settings = CementSettings.Get();
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

        private void BeforeRun()
        {
            startInfo.Arguments = Helper.OsIsUnix() ? " -lc " : " /D /C ";
            Output = "";
            Errors = "";
            HasTimeout = false;
        }

        public delegate void ReadLineEvent(string content);

        public event ReadLineEvent OnOutputChange, OnErrorsChange;

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

        private void ReadCmdOutput(Process pr)
        {
            Output = ReadStream(pr.StandardOutput, OnOutputChange);
        }

        private void ReadCmdError(Process pr)
        {
            Errors = ReadStream(pr.StandardError, OnErrorsChange);
        }

        private int RunThreeTimes(string commandWithArguments, string workingDirectory, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            int exitCode = RunOnce(commandWithArguments, workingDirectory, timeout);
            int times = 2;

            while (times-- > 0 && NeedRunAgain(retryStrategy, exitCode))
            {
                if (HasTimeout)
                    timeout = TimeoutHelper.IncreaseTimeout(timeout);
                exitCode = RunOnce(commandWithArguments, workingDirectory, timeout);
                log.LogDebug($"EXECUTED {startInfo.FileName} {startInfo.Arguments} in {workingDirectory} with exitCode {exitCode} and retryStrategy {retryStrategy}");
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

        public int RunOnce(string commandWithArguments, string workingDirectory, TimeSpan timeout)
        {
            BeforeRun();
            //var quote = Helper.OsIsUnix() ? ''' : '"';
            var quote = '"';
            startInfo.Arguments = startInfo.Arguments + quote + commandWithArguments + quote;
            startInfo.WorkingDirectory = workingDirectory;

            var sw = Stopwatch.StartNew();
            using (var process = Process.Start(startInfo))
            {
                try
                {
                    var threadOutput = new Thread(() => ReadCmdOutput(process));
                    var threadErrors = new Thread(() => ReadCmdError(process));
                    threadOutput.Start();
                    threadErrors.Start();

                    if (!threadOutput.Join(timeout) || !threadErrors.Join(timeout) || !process.WaitForExit((int) timeout.TotalMilliseconds))
                    {
                        threadOutput.Abort();
                        threadErrors.Abort();
                        KillProcessAndChildren(process.Id, new HashSet<int>());
                        HasTimeout = true;

                        var message = string.Format("Running timeout at {2} for command {0} in {1}", commandWithArguments, workingDirectory, timeout);
                        Errors += message;
                        throw new TimeoutException(message);
                    }

                    LastOutput = Output;
                    int exitCode = process.ExitCode;
                    log.LogInformation($"EXECUTED {startInfo.FileName} {startInfo.Arguments} in {workingDirectory} in {sw.ElapsedMilliseconds}ms with exitCode {exitCode}");
                    return exitCode;
                }
                catch (CementException e)
                {
                    if (e is TimeoutException)
                    {
                        if (!commandWithArguments.Equals("git ls-remote --heads"))
                            ConsoleWriter.WriteWarning(e.Message);
                        log?.LogWarning(e.Message);
                    }
                    else
                    {
                        ConsoleWriter.WriteError(e.Message);
                        log?.LogError(e.Message);
                    }
                    return -1;
                }
            }
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
                int child = Convert.ToInt32(mo["ProcessID"]);
                KillProcessAndChildren(child, killed);
            }

            try
            {
                var proc = Process.GetProcessById(pid);
                if (!IsCementProcess(proc.ProcessName))
                    return;

                log?.LogDebug("kill " + proc.ProcessName + "#" + proc.Id);
                proc.Kill();
            }
            catch (Exception exception)
            {
                log?.LogDebug("killing already exited process #" + pid, exception);
            }
        }

        private static bool IsCementProcess(string process)
        {
            return process == "cmd" || process.StartsWith("ssh") || process.StartsWith("git");
        }

        public int Run(string commandWithArguments)
        {
            return Run(commandWithArguments, DefaultTimeout);
        }

        public int Run(string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            return RunThreeTimes(commandWithArguments, Directory.GetCurrentDirectory(), timeout, retryStrategy);
        }

        public int RunInDirectory(string path, string commandWithArguments)
        {
            return RunInDirectory(path, commandWithArguments, DefaultTimeout);
        }

        public int RunInDirectory(string path, string commandWithArguments, TimeSpan timeout, RetryStrategy retryStrategy = RetryStrategy.IfTimeout)
        {
            return RunThreeTimes(commandWithArguments, path, timeout, retryStrategy);
        }
    }

    public enum RetryStrategy
    {
        None,
        IfTimeout,
        IfTimeoutOrFailed
    }

    public static class TimeoutHelper
    {
        private static readonly TimeSpan SmallTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan BigTimeout = TimeSpan.FromMinutes(10);
        private const int TimesForUseBigDefault = 1;

        private static int badTimes;

        public static TimeSpan IncreaseTimeout(TimeSpan was)
        {
            badTimes++;
            return was < BigTimeout ? BigTimeout : was;
        }

        public static TimeSpan GetStartTimeout()
        {
            if (badTimes > TimesForUseBigDefault)
                return BigTimeout;
            return SmallTimeout;
        }
    }
}