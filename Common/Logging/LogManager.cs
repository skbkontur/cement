using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Hercules.Client;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.Hercules;
using Vostok.Logging.Hercules.Configuration;
using Vostok.Logging.Microsoft;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Common.Logging
{
    public static class LogManager
    {
        private static ILoggerFactory loggerFactory;

        private static ILog fileLog;
        private static ILog herculesLog;

        static LogManager()
        {
            loggerFactory = LoggerFactory.Create(
                builder => builder
                    .SetMinimumLevel(LogLevel.None));
        }

        public static ILogger GetLogger(Type type) =>
            loggerFactory.CreateLogger(type);

        public static ILogger<T> GetLogger<T>() =>
            loggerFactory.CreateLogger<T>();

        public static ILogger GetLogger(string categoryName) =>
            loggerFactory.CreateLogger(categoryName);

        public static void SetDebugLoggingLevel()
        {
            SetMinimumLoggingLevel(LogLevel.Debug);
        }

        public static void SetMinimumLoggingLevel(LogLevel minLevel)
        {
            loggerFactory = LoggerFactory.Create(
                builder => builder
                    .SetMinimumLevel(minLevel));
        }

        public static void InitializeFileLogger(string logFileName)
        {
            if (!(fileLog is null))
                return;

            fileLog = GetFileLogger(logFileName);
            loggerFactory.AddVostok(fileLog);
        }

        public static void InitializeHerculesLogger()
        {
            if (!(herculesLog is null))
                return;

            var configLogFilePath = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "herculeslog.config.json");
            if (!File.Exists(configLogFilePath))
            {
                ConsoleWriter.WriteError($"{configLogFilePath} not found.");
                return;
            }

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configLogFilePath)
                .Build()
                .Get<HerculesLogConfiguration>();

            if (!configuration.HerculesLogEnabled)
                return;

            var settings = new HerculesSinkSettings(new FixedClusterProvider(configuration.ServerUrl), () => configuration.ApiKey)
            {
                MaximumMemoryConsumption = configuration.MaximumMemoryConsumptionInBytes
            };

            var herculesSink = new HerculesSink(settings, new SilentLog());

            herculesLog = new HerculesLog(new HerculesLogSettings(herculesSink, configuration.Stream))
                .WithProperties(
                    new Dictionary<string, object>
                    {
                        ["project"] = configuration.Project,
                        ["environment"] = configuration.Environment
                    });

            loggerFactory.AddVostok(herculesLog);
        }

        public static void DisposeFileLogger()
        {
            (fileLog as IDisposable)?.Dispose();
        }

        public static void DisposeHerculesLogger()
        {
            (herculesLog as IDisposable)?.Dispose();
        }

        public static void DisposeLoggers()
        {
            DisposeFileLogger();
            DisposeHerculesLogger();
        }

        private static ILog GetFileLogger(string logFileName)
        {
            logFileName = logFileName == null
                ? Path.Combine(Helper.GetGlobalCementDirectory(), "log", "log")
                : Path.Combine(Helper.CurrentWorkspace, Helper.CementDirectory, "log", logFileName);

            Environment.SetEnvironmentVariable("logfilename", logFileName);
            return new FileLog(
                new FileLogSettings
                {
                    RollingStrategy = new RollingStrategyOptions
                    {
                        Type = RollingStrategyType.None
                    },
                    FilePath = logFileName
                });
        }
    }
}