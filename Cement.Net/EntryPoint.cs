using System;
using System.Linq;
using System.Threading;
using Commands;
using Common;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace cm
{
    internal static class EntryPoint
    {
        private static ILogger logger;

        private static int Main(string[] args)
        {
            LogManager.SetDebugLoggingLevel();

            logger = LogManager.GetLogger(typeof(EntryPoint));

            ThreadPoolSetUp(Helper.MaxDegreeOfParallelism);
            args = FixArgs(args);
            var exitCode = TryRun(args);

            ConsoleWriter.Shared.ResetProgress();

            var command = args[0];
            if (command != "complete" && command != "check-pre-commit"
                                      && (command != "help" || !args.Contains("--gen")))
                SelfUpdate.UpdateIfOld();

            logger.LogInformation($"Exit code: {exitCode}");

            LogManager.DisposeLoggers();

            return exitCode == 0 ? 0 : 13;
        }

        private static string[] FixArgs(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("cm"))
                args = args.Skip(1).ToArray();
            if (args.Length == 0)
                args = new[] {"help"};
            if (args.Contains("--help") || args.Contains("/?"))
                args = new[] {"help", args[0]};
            return args;
        }

        private static int TryRun(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (CementException e)
            {
                ConsoleWriter.Shared.WriteError(e.Message);
                logger.LogError(e, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException is CementException cementException)
                {
                    ConsoleWriter.Shared.WriteError(cementException.Message);
                    logger.LogError(e.InnerException, e.InnerException.Message);
                }
                else
                {
                    ConsoleWriter.Shared.WriteError(e.Message);
                    ConsoleWriter.Shared.WriteError(e.StackTrace);
                    logger.LogError(e, e.Message);
                }

                return -1;
            }
        }

        private static int Run(string[] args)
        {
            var commands = CommandsList.Commands;
            if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]].Run(args);
            }

            if (CementSettingsRepository.Get().UserCommands.ContainsKey(args[0]))
                return new UserCommand().Run(args);

            ConsoleWriter.Shared.WriteError("Bad command: '" + args[0] + "'");
            return -1;
        }

        private static void ThreadPoolSetUp(int count)
        {
            int num = Math.Min(count, short.MaxValue);
            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(num, num);
        }
    }
}
