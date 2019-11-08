using System;
using System.Collections.Generic;
using System.IO;
using Common.ClusterConfigProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        public static ILogger GetLogger(Type type)
        {
            return loggerFactory.CreateLogger(type);
        }

        public static ILogger<T> GetLogger<T>()
        {
            return loggerFactory.CreateLogger<T>();
        }

        public static ILogger GetLogger(string categoryName)
        {
            return loggerFactory.CreateLogger(categoryName);
        }

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
            var configLogFilePath = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "herculeslog.config.json");

            if (!(herculesLog is null) || !File.Exists(configLogFilePath))
                return;

            var logConfiguration = new ConfigurationBuilder()
                    .AddJsonFile(configLogFilePath)
                    .Build()
                    .Get<HerculesLogConfiguration>();

            var settings = new HerculesSinkSettings(new FixedUrlClusterProvider(logConfiguration.ServerUrl), () => logConfiguration.ApiKey)
            {
                MaximumMemoryConsumption = logConfiguration.MaximumMemoryConsumptionInBytes
            };

            var herculesSink = new HerculesSink(settings, new SilentLog());

            herculesLog = new HerculesLog(new HerculesLogSettings(herculesSink, logConfiguration.Stream))
                .WithProperties(
                    new Dictionary<string, object>
                    {
                        ["project"] = logConfiguration.Project,
                        ["environment"] = logConfiguration.Environment
                    });

            loggerFactory.AddVostok(herculesLog);
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