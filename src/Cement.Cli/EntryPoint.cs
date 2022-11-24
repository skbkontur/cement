﻿using System;
using System.Linq;
using System.Threading;
using Cement.Cli.Commands;
using Cement.Cli.Commands.Common;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Logging;
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

            LogManager.InitializeFileLogger();
            LogManager.InitializeHerculesLogger();

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
            services.AddSingleton<ICommandActivator, DefaultCommandActivator>();

            services.AddSingleton(consoleWriter);
            services.AddSingleton(featureFlags);

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

            var options = new ServiceProviderOptions
            {
#if DEBUG
                ValidateOnBuild = true,
                ValidateScopes = true
#endif
            };

            var sp = services.BuildServiceProvider(options);

            logger = sp.GetRequiredService<ILogger<EntryPoint>>();
            logger.LogInformation("Cement version: {CementVersion}", Helper.GetAssemblyTitle());

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
            catch (Exception ex)
            {
                consoleWriter.WriteError(ex.ToString());
                logger.LogError(ex, "An unknown error has occurred");

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