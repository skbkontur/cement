using System;
using System.Linq;
using System.Threading;
using Commands;
using Common;
using Common.DepsValidators;
using Common.Exceptions;
using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace cm
{
    internal sealed class EntryPoint
    {
        private static ILogger logger;

        private static int Main(string[] args)
        {
            args = FixArgs(args);
            ThreadPoolSetUp(Helper.MaxDegreeOfParallelism);

            var consoleWriter = ConsoleWriter.Shared;

            var featureFlagsProvider = new FeatureFlagsProvider(consoleWriter);
            var featureFlags = featureFlagsProvider.Get();

            var services = new ServiceCollection();
            services.AddSingleton(consoleWriter);
            services.AddSingleton(typeof(ILogger<>), typeof(DefaultLogger<>));
            services.AddSingleton(typeof(Lazy<>), typeof(Lazy<>));
            services.AddSingleton<ReadmeGenerator>();

            services.AddSingleton<CycleDetector>();
            services.AddSingleton(CementSettingsRepository.Get);
            services.AddSingleton<IUsagesProvider, UsagesProvider>();
            services.AddSingleton<IDepsValidatorFactory>(DepsValidatorFactory.Shared);
            services.AddSingleton<ModuleHelper>();
            services.AddSingleton<DepsPatcherProject>();
            services.AddSingleton<IGitRepositoryFactory, GitRepositoryFactory>();
            services.AddSingleton<CompleteCommandAutomata>();

            services.AddSingleton(consoleWriter);
            services.AddSingleton(featureFlags);

            services.AddSingleton<CycleDetector>();
            services.AddSingleton(BuildPreparer.Shared);
            services.AddSingleton(BuildHelper.Shared);

            services.AddCommand<HelpCommand>();
            services.AddCommand<GetCommand>();
            services.AddCommand<UpdateDepsCommand>();
            services.AddCommand<RefCommand>();
            services.AddCommand<AnalyzerCommand>();
            services.AddCommand<LsCommand>();
            services.AddCommand<ShowConfigsCommand>();
            services.AddCommand<ShowDepsCommand>();
            services.AddCommand<SelfUpdateCommand>();
            services.AddCommand<VersionCommand>();
            services.AddCommand<BuildDepsCommand>();
            services.AddCommand<BuildCommand>();
            services.AddCommand<CheckDepsCommand>();
            services.AddCommand<CheckPreCommitCommand>();
            services.AddCommand<UsagesCommand>();
            services.AddCommand<InitCommand>();
            services.AddCommand<IdCommand>();
            services.AddCommand<StatusCommand>();
            services.AddCommand<ModuleCommand>();
            services.AddCommand<UpdateCommand>();
            services.AddCommand<ConvertSpecCommand>();
            services.AddCommand<ReInstallCommand>();
            services.AddCommand<CompleteCommand>();
            services.AddCommand<PackCommand>();
            services.AddCommand<PackagesCommand>();
            services.AddCommand<UserCommand>();

            var options = new ServiceProviderOptions
            {
#if DEBUG
                ValidateOnBuild = true,
                ValidateScopes = true
#endif
            };

            var sp = services.BuildServiceProvider(options);

            logger = sp.GetRequiredService<ILogger<EntryPoint>>();

            var exitCode = TryRun(consoleWriter, sp, args);

            consoleWriter.ResetProgress();

            var command = args[0];
            if (command != "complete" && command != "check-pre-commit"
                                      && (command != "help" || !args.Contains("--gen")))
            {
                SelfUpdate.UpdateIfOld(consoleWriter, featureFlags);
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

        private static int TryRun(ConsoleWriter consoleWriter, IServiceProvider sp, string[] args)
        {
            try
            {
                return Run(consoleWriter, sp, args);
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

        private static int Run(ConsoleWriter consoleWriter, IServiceProvider sp, string[] args)
        {
            var commands = sp.GetServices<ICommand>()
                .ToDictionary(c => c.Name);

            if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]].Run(args);
            }

            if (CementSettingsRepository.Get().UserCommands.ContainsKey(args[0]))
            {
                var userCommand = sp.GetRequiredService<UserCommand>();
                return userCommand.Run(args);
            }

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
