using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Common.Console;

namespace Common;

public sealed class ConsoleWriter
{
    private readonly object lockObject = new();

    private readonly IConsole @out = IsTerminalSupportsAnsi()
        ? new AnsiConsole(System.Console.Out)
        : new WindowsConsole(System.Console.Out);

    private readonly IConsole error = IsTerminalSupportsAnsi()
        ? new AnsiConsole(System.Console.Error)
        : new WindowsConsole(System.Console.Error);

    private readonly Stack<string> progressMessageStack = new();
    private readonly HashSet<string> processedModules = new();
    public static ConsoleWriter Shared { get; } = new();

    private static bool IsTerminalSupportsAnsi()
    {
        // dstarasov: временно отключил, из-за проблем на macOS
        return false;

        //return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public void WriteProgress(string progress)
    {
        if (System.Console.IsOutputRedirected)
            return;

        Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan);
    }

    public void WriteProgressWithoutSave(string progress)
    {
        if (System.Console.IsOutputRedirected)
            return;

        Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan, false);
    }

    public void WriteInfo(string info)
    {
        PrintLn(INFO + info, ConsoleColor.White);
    }

    public void WriteSkip(string text)
    {
        PrintLn(SKIP + text, ConsoleColor.Gray);
    }

    public void WriteUpdate(string text)
    {
        PrintLn(UPDATE + text, ConsoleColor.Green);
    }

    public void WriteOk(string text)
    {
        PrintLn(OK + text, ConsoleColor.Green);
    }

    public void WriteLines(IEnumerable<string> lines)
    {
        PrintLn(string.Join(Environment.NewLine, lines));
    }

    public void WriteLine(string text)
    {
        PrintLn(text);
    }

    public void WriteLine(string format, params object[] args)
    {
        PrintLn(string.Format(format, args));
    }

    public void WriteLine()
    {
        PrintLn(string.Empty);
    }

    public void Write(string text)
    {
        Print(text);
    }

    public void Write(string format, params object[] args)
    {
        Print(string.Format(format, args));
    }

    public void WriteWarning(string warning)
    {
        PrintLnError(WARNING + warning, ConsoleColor.Yellow);
    }

    public void WriteBuildWarning(string warning)
    {
        PrintLnError(warning, ConsoleColor.Yellow);
    }

    public void WriteLineBuildWarning(string warning)
    {
        PrintLnError(warning, ConsoleColor.Yellow, true);
    }

    public void WriteError(string error)
    {
        PrintLnError(ERROR + error, ConsoleColor.Red);
    }

    public void WriteBuildError(string error)
    {
        PrintLnError(error, ConsoleColor.Red);
    }

    public void ClearLine()
    {
        if (System.Console.IsOutputRedirected)
            return;

        @out.ClearLine();
    }

    public void PrintLn(string text, ConsoleColor? foregroundColor = default)
    {
        lock (lockObject)
        {
            ClearLine();

            @out.Write(text, foregroundColor: foregroundColor);
            @out.Write(Environment.NewLine);

            SaveToProcessedModules(text);
        }
    }

    public void SaveToProcessedModules(string text)
    {
        lock (lockObject)
        {
            var moduleName = text.Split(']').Last().Trim().Split(' ', '/').First();
            processedModules.Add(moduleName);
            PrintLastProgressFromStack();
        }
    }

    public void ResetProgress()
    {
        progressMessageStack.Clear();
        processedModules.Clear();
        ClearLine();
    }

    private string TakeNoMoreThanConsoleWidth(string line)
    {
        var consoleWindowWidth = @out.WindowWidth;
        return line.Length < consoleWindowWidth - 1 ? line : line.Substring(0, consoleWindowWidth - 1);
    }

    private void PrintLnError(string text, ConsoleColor foregroundColor = default, bool emptyLineAfter = false)
    {
        lock (lockObject)
        {
            if (!System.Console.IsErrorRedirected)
                error.ClearLine();

            error.Write(text, foregroundColor: foregroundColor);
            error.Write(Environment.NewLine);

            if (emptyLineAfter)
                error.Write(Environment.NewLine);
        }
    }

    private void Print(string text, ConsoleColor? foregroundColor = default, bool saveProgress = true)
    {
        lock (lockObject)
        {
            @out.ClearLine();
            @out.Write(text, foregroundColor: foregroundColor);

            if (text.StartsWith(PROGRESS) && saveProgress)
                progressMessageStack.Push(text);
        }
    }

    private void PrintLastProgressFromStack()
    {
        while (progressMessageStack.Count > 0)
        {
            var topQueueModuleName = progressMessageStack.Peek().Replace(PROGRESS, "").Trim().Split(' ', '/').First();
            if (processedModules.Contains(topQueueModuleName))
                progressMessageStack.Pop();
            else
            {
                WriteProgress(progressMessageStack.Pop().Replace(PROGRESS + " ", ""));
                break;
            }
        }
    }

    // ReSharper disable InconsistentNaming
    private const string PROGRESS = "[PROGRESS] ";

    private const string OK = "[ OK ] ";
    private const string UPDATE = "[UPDT] ";
    private const string SKIP = "[SKIP] ";
    private const string WARNING = "[WARN] ";
    private const string ERROR = "[ERROR] ";

    private const string INFO = "[INFO] ";
    // ReSharper restore InconsistentNaming
}
