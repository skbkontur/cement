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

namespace Cement.Cli.Common.Logging;

public static class LogManager
{
    private static readonly List<IDisposable> Disposables = new();

    private static readonly ILoggerFactory LoggerFactory;
    private static ILog fileLog;
    private static ILog herculesLog;

    static LogManager()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
            builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
    }

    public static ILogger GetLogger(Type type) =>
        LoggerFactory.CreateLogger(type);

    public static ILogger<T> GetLogger<T>() =>
        LoggerFactory.CreateLogger<T>();

    public static ILogger GetLogger(string categoryName) =>
        LoggerFactory.CreateLogger(categoryName);

    public static void InitializeFileLogger()
    {
        if (fileLog is not null)
            return;

        fileLog = GetFileLogger("cement.cli.log");
        LoggerFactory.AddVostok(fileLog);
    }

    public static void InitializeHerculesLogger()
    {
        if (herculesLog is not null)
            return;

        var configLogFilePath = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "herculeslog.config.json");
        if (!File.Exists(configLogFilePath))
        {
            ConsoleWriter.Shared.WriteError($"{configLogFilePath} not found.");
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

        var fileLogForHercules = GetFileLogger("hercules-sink.log");
        var herculesSink = new HerculesSink(settings, fileLogForHercules);
        Disposables.Add(herculesSink);

        herculesLog = new HerculesLog(new HerculesLogSettings(herculesSink, configuration.Stream))
            .WithProperties(
                new Dictionary<string, object>
                {
                    ["project"] = configuration.Project,
                    ["environment"] = configuration.Environment,
                    ["instance"] = ObtainHostname()
                });

        LoggerFactory.AddVostok(herculesLog);
    }

    public static void DisposeLoggers()
    {
        Disposables.Reverse();
        foreach (var disposable in Disposables)
            disposable?.Dispose();
    }

    private static ILog GetFileLogger(string fileName)
    {
        var logFileName = Path.Combine(Helper.LogsDirectory(), fileName);
        var fileLogSettings = new FileLogSettings
        {
            RollingStrategy = new RollingStrategyOptions
            {
                Type = RollingStrategyType.ByTime,
                MaxFiles = 10
            },
            FilePath = logFileName
        };

        var result = new FileLog(fileLogSettings);
        Disposables.Add(result);

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
