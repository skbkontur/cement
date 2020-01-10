using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        private static List<IDisposable> disposables = new List<IDisposable>();

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

        public static void InitializeHerculesLogger(string command)
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

            if (!configuration.Enabled)
                return;

            var settings = new HerculesSinkSettings(new FixedClusterProvider(configuration.ServerUrl), () => configuration.ApiKey)
            {
                MaximumMemoryConsumption = configuration.MaximumMemoryConsumptionInBytes
            };

            var fileLogForHercules = GetFileLogger("hercules");
            var herculesSink = new HerculesSink(settings, fileLogForHercules);

            herculesLog = new HerculesLog(new HerculesLogSettings(herculesSink, configuration.Stream))
                .WithProperties(
                    new Dictionary<string, object>
                    {
                        ["project"] = configuration.Project,
                        ["environment"] = configuration.Environment,
                        ["hostName"] = ObtainHostname(),
                        ["command"] = command
                    });

            disposables.Add(herculesSink);

            loggerFactory.AddVostok(herculesLog);
        }
        
        public static void DisposeLoggers()
        {
            disposables.Reverse();
            foreach (var disposable in disposables)
                disposable?.Dispose();
        }

        private static ILog GetFileLogger(string logFileName)
        {
            logFileName = logFileName == null
                ? Path.Combine(Helper.GetGlobalCementDirectory(), "log", "log")
                : Path.Combine(Helper.CurrentWorkspace, Helper.CementDirectory, "log", logFileName);

            if (!logFileName.EndsWith(".log"))
                logFileName += ".log";

            Environment.SetEnvironmentVariable("logfilename", logFileName);
            var result = new FileLog(
                new FileLogSettings
                {
                    RollingStrategy = new RollingStrategyOptions
                    {
                        Type = RollingStrategyType.ByTime,
                        MaxFiles = 10
                    },
                    FilePath = logFileName
                });

            disposables.Add(result);

            return result;
        }

        private static string ObtainHostname()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return "unknown";
            }
        }
    }
}