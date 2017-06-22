using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class ConsoleWriter
    {
        private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;
        private static readonly int ConsoleWindowWidth = CalculateWindowWidth();

        private static readonly Stack<string> ProgressMessageStack = new Stack<string>();

        private static readonly HashSet<string> ProcessedModules = new HashSet<string>();

        // ReSharper disable InconsistentNaming
        private const string PROGRESS = "[PROGRESS] ";

        private const string OK = "[ OK ] ";
        private const string UPDATE = "[UPDT] ";
        private const string SKIP = "[SKIP] ";
        private const string WARNING = "[WARN] ";
        private const string ERROR = "[ERROR] ";

        private const string INFO = "[INFO] ";
        // ReSharper restore InconsistentNaming

        private static int CalculateWindowWidth()
        {
            var result = 80;
            try
            {
                result = Console.WindowWidth;
            }
            catch
            {
                // ignored
            }
            return result;
        }

        public static void WriteProgress(string progress)
        {
            if (!Console.IsOutputRedirected)
                Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan);
        }

        public static void WriteProgressWithoutSave(string progress)
        {
            if (!Console.IsOutputRedirected)
            {
                Print(TakeNoMoreThanConsoleWidth(PROGRESS + " " + progress), ConsoleColor.Cyan, false);
            }
        }

        private static string TakeNoMoreThanConsoleWidth(string line)
        {
            return line.Length < ConsoleWindowWidth - 1 ? line : line.Substring(0, ConsoleWindowWidth - 1);
        }

        public static void WriteInfo(string info)
        {
            PrintLn(INFO + info, ConsoleColor.White);
        }

        public static void WriteSkip(string text)
        {
            PrintLn(SKIP + text, ConsoleColor.DarkGray);
        }

        public static void WriteUpdate(string text)
        {
            PrintLn(UPDATE + text, ConsoleColor.Green);
        }

        public static void WriteOk(string text)
        {
            PrintLn(OK + text, ConsoleColor.Green);
        }

        public static void WriteLine(string text)
        {
            PrintLn(text, DefaultColor);
        }

        public static void WriteLine()
        {
            PrintLn("", DefaultColor);
        }

        public static void Write(string text)
        {
            Print(text, DefaultColor);
        }

        public static void WriteWarning(string warning)
        {
            PrintLnError(WARNING + warning, ConsoleColor.Yellow);
        }

        public static void WriteBuildWarning(string warning)
        {
            PrintLnError(warning, ConsoleColor.Yellow);
        }

        public static void WriteLineBuildWarning(string warning)
        {
            PrintLnError(warning, ConsoleColor.Yellow, true);
        }

        public static void WriteError(string error)
        {
            PrintLnError(ERROR + error, ConsoleColor.Red);
        }

        public static void WriteBuildError(string error)
        {
            PrintLnError(error, ConsoleColor.Red);
        }

        private static void PrintLnError(string text, ConsoleColor color, bool emptyLineAfter = false)
        {
            lock (Helper.LockObject)
            {
                Console.ForegroundColor = color;
                if (!Console.IsOutputRedirected)
                    Console.Error.Write("\r{0,-" + $"{ConsoleWindowWidth - 1}" + "}\r", "");
                Console.Error.WriteLine(text);
                if (emptyLineAfter)
                    Console.Error.WriteLine();
                Console.ForegroundColor = DefaultColor;
            }
        }

        public static void ClearLine()
        {
            if (Console.IsOutputRedirected)
                return;
            Console.Write("\r{0,-" + $"{ConsoleWindowWidth - 1}" + "}\r", "");
        }

        private static void Print(string text, ConsoleColor color, bool saveProgress = true)
        {
            lock (Helper.LockObject)
            {
                Console.ForegroundColor = color;
                ClearLine();
                Console.Write(text);
                Console.ForegroundColor = DefaultColor;
                if (text.StartsWith(PROGRESS) && saveProgress)
                    ProgressMessageStack.Push(text);
            }
        }

        public static void PrintLn(string text, ConsoleColor color)
        {
            lock (Helper.LockObject)
            {
                Console.ForegroundColor = color;
                ClearLine();
                Console.WriteLine(text);
                Console.ForegroundColor = DefaultColor;
                SaveToProcessedModules(text);
            }
        }

        public static void SaveToProcessedModules(string text)
        {
            lock (Helper.LockObject)
            {
                var moduleName = text.Split(']').Last().Trim().Split(' ', '/').First();
                ProcessedModules.Add(moduleName);
                PrintLastProgressFromStack();
            }
        }

        private static void PrintLastProgressFromStack()
        {
            while (ProgressMessageStack.Count > 0)
            {
                var topQueueModuleName = ProgressMessageStack.Peek().Replace(PROGRESS, "").Trim().Split(' ', '/').First();
                if (ProcessedModules.Contains(topQueueModuleName))
                    ProgressMessageStack.Pop();
                else
                {
                    WriteProgress(ProgressMessageStack.Pop().Replace(PROGRESS + " ", ""));
                    break;
                }
            }
        }

        public static void ResetProgress()
        {
            ProgressMessageStack.Clear();
            ProcessedModules.Clear();
            ClearLine();
        }
    }
}
