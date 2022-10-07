using System;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Commands;

[PublicAPI]
public sealed class DefaultLogger<T> : ILogger<T>
{
    private readonly ILogger<T> logger;

    public DefaultLogger()
    {
        logger = LogManager.GetLogger<T>();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                            Func<TState, Exception, string> formatter)
    {
        logger.Log(logLevel, eventId, state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

    public IDisposable BeginScope<TState>(TState state) => logger.BeginScope(state);
}
