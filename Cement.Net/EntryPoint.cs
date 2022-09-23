using System;
using System.Linq;
using System.Threading;
using Commands;
using Common;
using Common.Exceptions;
using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace cm
{
    internal static class EntryPoint
    {
        private static ILogger logger;

        private static int Main(string[] args)
        {
            args = FixArgs(args);
            ThreadPoolSetUp(Helper.MaxDegreeOfParallelism);

            var consoleWriter = ConsoleWriter.Shared;

            var services = new ServiceCollection();
            services.AddSingleton(consoleWriter);

            logger = LogManager.GetLogger(typeof(EntryPoint));

            var featureFlagsProvider = new FeatureFlagsProvider(consoleWriter);
            var featureFlags = featureFlagsProvider.Get();

            var exitCode = TryRun(consoleWriter, featureFlags, args);

            consoleWriter.ResetProgress();

            var command = args[0];
            if (command != "complete" && command != "check-pre-commit"
                                      && (command != "help" || !args.Contains("--gen")))
            {
                SelfUpdateCommand.UpdateIfOld(featureFlags);
            }

            logger.LogInformation("Exit code: {ExitCode}", exitCode);
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

        private static int TryRun(ConsoleWriter consoleWriter, FeatureFlags featureFlags, string[] args)
        {
            try
            {
                return Run(consoleWriter, featureFlags, args);
            }
            catch (CementException e)
            {
                consoleWriter.WriteError(e.Message);
                logger.LogError(e, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException is CementException cementException)
                {
                    consoleWriter.WriteError(cementException.Message);
                    logger.LogError(e.InnerException, e.InnerException.Message);
                }
                else
                {
                    consoleWriter.WriteError(e.Message);
                    consoleWriter.WriteError(e.StackTrace);
                    logger.LogError(e, e.Message);
                }

                return -1;
            }
        }

        private static int Run(ConsoleWriter consoleWriter, FeatureFlags featureFlags, string[] args)
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var commands = new CommandsList(consoleWriter, featureFlags);
            if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]].Run(args);
            }

            if (CementSettingsRepository.Get().UserCommands.ContainsKey(args[0]))
                return new UserCommand(consoleWriter, featureFlags).Run(args);

            consoleWriter.WriteError("Bad command: '" + args[0] + "'");
            return -1;
        }

        private static void ThreadPoolSetUp(int count)
        {
            var num = Math.Min(count, short.MaxValue);
            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(num, num);
        }
    }
}
