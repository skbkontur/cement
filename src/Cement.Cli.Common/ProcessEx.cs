#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Cement.Cli.Common;

internal sealed class ProcessEx : IDisposable
{
    private readonly Process nativeProcess;

    public ProcessEx(ProcessStartInfo startInfo)
    {
        nativeProcess = new Process {StartInfo = startInfo};
    }

    public int Id { get; private set; }

    public bool IsExited { get; private set; }

    public bool IsKilled { get; private set; }

    public StreamWriter StandardInput { get; private set; } = StreamWriter.Null;

    public StreamReader StandardOutput { get; private set; } = StreamReader.Null;

    public StreamReader StandardError { get; private set; } = StreamReader.Null;

    public int ExitCode { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset ExitTime { get; private set; }

    public void Start()
    {
        nativeProcess.EnableRaisingEvents = true;
        nativeProcess.Exited += (_, _) =>
        {
            IsExited = true;
            ExitCode = nativeProcess.ExitCode;
            ExitTime = DateTimeOffset.Now;
        };

        try
        {
            if (!nativeProcess.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start a process with file path '{nativeProcess.StartInfo.FileName}'. " +
                    "Target file is not an executable or lacks execute permissions."
                );
            }
        }
        catch (Win32Exception ex)
        {
            throw new Win32Exception(
                $"Failed to start a process with file path '{nativeProcess.StartInfo.FileName}'. " +
                "Target file or working directory doesn't exist, or the provided credentials are invalid.",
                ex
            );
        }

        StartTime = DateTimeOffset.Now;
        Id = nativeProcess.Id;
        StandardInput = nativeProcess.StandardInput;
        StandardOutput = nativeProcess.StandardOutput;
        StandardError = nativeProcess.StandardError;
    }

    public void Kill()
    {
        try
        {
            IsKilled = true;
            nativeProcess.Kill(true);
        }
        catch (Exception)
        {
            //
        }
    }

    public bool WaitForExit(int milliseconds)
    {
        return nativeProcess.WaitForExit(milliseconds);
    }

    public void Dispose()
    {
        nativeProcess.Dispose();
    }
}
