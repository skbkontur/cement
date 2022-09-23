using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common
{
    public sealed class ConsoleWriter
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        private readonly ConsoleColor defaultColor = Console.ForegroundColor;
        private readonly Stack<string> progressMessageStack = new();
        private readonly HashSet<string> processedModules = new();
        public static ConsoleWriter Shared { get; } = new();

        public void WriteProgress(string progress)
        {
            if (!Console.IsOutputRedirected)
                Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan);
        }

        public void WriteProgressWithoutSave(string progress)
        {
            if (!Console.IsOutputRedirected)
            {
                Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan, false);
            }
        }

        public void WriteInfo(string info)
        {
            PrintLn(INFO + info, ConsoleColor.White);
        }

        public void WriteSkip(string text)
        {
            PrintLn(SKIP + text, ConsoleColor.DarkGray);
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
            PrintLn(string.Join(Environment.NewLine, lines), defaultColor);
        }

        public void WriteLine(string text)
        {
            PrintLn(text, defaultColor);
        }

        public void WriteLine(string format, params object[] args)
        {
            PrintLn(string.Format(format, args), defaultColor);
        }

        public void WriteLine()
        {
            PrintLn("", defaultColor);
        }

        public void Write(string text)
        {
            Print(text, defaultColor);
        }

        public void Write(string format, params object[] args)
        {
            Print(string.Format(format, args), defaultColor);
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
            if (Console.IsOutputRedirected)
                return;

            var consoleWindowWidth = CalculateWindowWidth();
            Console.Write($"\r{{0,-{consoleWindowWidth - 1}}}\r", "");
        }

        public void PrintLn(string text, ConsoleColor color)
        {
            semaphore.Wait();
            try
            {
                UnsafePrintLn(text, color);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void UnsafePrintLn(string text, ConsoleColor color)
        {
            ClearLine();

            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = defaultColor;

            UnsafeSaveToProcessedModules(text);
        }

        public void SaveToProcessedModules(string text)
        {
            semaphore.Wait();
            try
            {
                UnsafeSaveToProcessedModules(text);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void UnsafeSaveToProcessedModules(string text)
        {
            var moduleName = text.Split(']').Last().Trim().Split(' ', '/').First();
            processedModules.Add(moduleName);
            PrintLastProgressFromStack();
        }

        public void ResetProgress()
        {
            progressMessageStack.Clear();
            processedModules.Clear();
            ClearLine();
        }

        private int CalculateWindowWidth()
        {
            var result = 80;
            try
            {
                result = Math.Max(result, Console.WindowWidth);
            }
            catch
            {
                // ignored
            }

            return result;
        }

        private string TakeNoMoreThanConsoleWidth(string line)
        {
            var consoleWindowWidth = CalculateWindowWidth();
            return line.Length < consoleWindowWidth - 1 ? line : line.Substring(0, consoleWindowWidth - 1);
        }

        private void PrintLnError(string text, ConsoleColor color, bool emptyLineAfter = false)
        {
            var consoleWindowWidth = CalculateWindowWidth();

            semaphore.Wait();
            try
            {
                Console.ForegroundColor = color;
                if (!Console.IsErrorRedirected)
                    Console.Error.Write($"\r{{0,-{consoleWindowWidth - 1}}}\r", "");
                Console.Error.WriteLine(text);
                if (emptyLineAfter)
                    Console.Error.WriteLine();
                Console.ForegroundColor = defaultColor;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void Print(string text, ConsoleColor color, bool saveProgress = true)
        {
            semaphore.Wait();
            try
            {
                Console.ForegroundColor = color;
                ClearLine();
                Console.Write(text);
                Console.ForegroundColor = defaultColor;
                if (text.StartsWith(PROGRESS) && saveProgress)
                    progressMessageStack.Push(text);
            }
            finally
            {
                semaphore.Release();
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
}
