using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cement.Cli.Commands;
using Cement.Cli.Commands.Common;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace cm
{
    internal sealed class EntryPoint
    {
        private const string Header = @"
                                     _
                                    | |
  ___   ___  _ __ ___    ___  _ __  | |_
 / __| / _ \| '_ ` _ \  / _ \| '_ \ | __|
| (__ |  __/| | | | | ||  __/| | | || |_
 \___| \___||_| |_| |_| \___||_| |_| \__|
";

        private static int Main(string[] args)
        {
            args = FixArgs(args);
            ThreadPoolSetUp(Helper.MaxDegreeOfParallelism);

            LogManager.InitializeFileLogger();
            LogManager.InitializeHerculesLogger();

            var services = new ServiceCollection();
            ConfigureServices(services);

            var options = new ServiceProviderOptions
            {
#if DEBUG
                ValidateOnBuild = true,
                ValidateScopes = true
#endif
            };

            var sp = services.BuildServiceProvider(options);

            var consoleWriter = sp.GetRequiredService<ConsoleWriter>();
            var logger = sp.GetRequiredService<ILogger<EntryPoint>>();
            logger.LogInformation("{CementHeader}{CementVersion}\n", Header, Helper.GetAssemblyTitle());

            var exitCode = TryRun(logger, consoleWriter, sp, args);

            consoleWriter.ResetProgress();

            var command = args[0];
            if (command != "complete" && command != "check-pre-commit"
                                      && (command != "help" || !args.Contains("--gen")))
            {
                SelfUpdate.UpdateIfOld(consoleWriter);
            }

            logger.LogInformation("Exit code: {ExitCode}", exitCode);
            LogManager.DisposeLoggers();

            return exitCode == 0 ? 0 : 13;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var consoleWriter = ConsoleWriter.Shared;
            services.AddSingleton(consoleWriter);

            var featureFlagsProvider = new FeatureFlagsProvider(consoleWriter);
            var featureFlags = featureFlagsProvider.Get();
            services.AddSingleton(featureFlags);

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
            services.AddSingleton<ICommandActivator, DefaultCommandActivator>();

            services.AddSingleton<CycleDetector>();
            services.AddSingleton(BuildPreparer.Shared);
            services.AddSingleton(BuildHelper.Shared);
            services.AddSingleton<IPackageUpdater>(PackageUpdater.Shared);
            services.AddSingleton<HooksHelper>();
            services.AddSingleton<ShellRunner>();

            services.AddCommand<HelpCommand>();
            services.AddCommand<GetCommand>();
            services.AddCommand<UpdateDepsCommand>();
            services.AddCommand<RefCommand>();
            services.AddSubcommand<RefAddCommand>();
            services.AddSubcommand<RefFixCommand>();

            services.AddCommand<AnalyzerCommand>();
            services.AddSubcommand<AnalyzerAddCommand>();

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
            services.AddSubcommand<UsagesShowCommand>();
            services.AddSubcommand<UsagesBuildCommand>();
            services.AddSubcommand<UsagesGrepCommand>();

            services.AddCommand<InitCommand>();
            services.AddCommand<IdCommand>();
            services.AddCommand<StatusCommand>();

            services.AddCommand<ModuleCommand>();
            services.AddSubcommand<AddModuleCommand>();
            services.AddSubcommand<ChangeModuleCommand>();

            services.AddCommand<UpdateCommand>();
            services.AddCommand<ReInstallCommand>();
            services.AddCommand<CompleteCommand>();
            services.AddCommand<PackCommand>();

            services.AddCommand<PackagesCommand>();
            services.AddSubcommand<AddPackageCommand>();
            services.AddSubcommand<ListPackagesCommand>();
            services.AddSubcommand<RemovePackageCommand>();

            services.AddCommand<UserCommand>();
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

        private static int TryRun(ILogger logger, ConsoleWriter consoleWriter, IServiceProvider sp, string[] args)
        {
            try
            {
                return Run(logger, consoleWriter, sp, args);
            }
            catch (CementException cex)
            {
                logger.LogError(cex, "An error has occurred");
                consoleWriter.WriteError(cex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                consoleWriter.WriteError(ex.ToString());
                logger.LogError(ex, "An unknown error has occurred");

                return -1;
            }

            static int Run(ILogger logger, ConsoleWriter consoleWriter, IServiceProvider sp, string[] args)
            {
                var command = GetCommand(sp, args);
                if (command == null)
                {
                    consoleWriter.WriteError("Bad command: '" + args[0] + "'");
                    return -1;
                }

                var sw = Stopwatch.StartNew();

                var exitCode = command.Run(args);

                if (!command.MeasureElapsedTime)
                    return exitCode;

                consoleWriter.WriteInfo("Total time: " + sw.Elapsed);
                logger.LogDebug("Total time: {TotalElapsed:c}", sw.Elapsed);

                return exitCode;
            }
        }

        private static ICommand? GetCommand(IServiceProvider sp, string[] args)
        {
            var commands = sp.GetServices<ICommand>()
                .ToDictionary(c => c.Name);

            if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]];
            }

            var cementSettings = CementSettingsRepository.Get();
            if (cementSettings.UserCommands.ContainsKey(args[0]))
            {
                var userCommand = sp.GetRequiredService<UserCommand>();
                return userCommand;
            }

            return null;
        }

        private static void ThreadPoolSetUp(int count)
        {
            var num = Math.Min(count, short.MaxValue);
            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(num, num);
        }
    }
}
