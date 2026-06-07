using System.Globalization;

namespace TrxToSonar.Logging;

/// <summary>
///     Minimal, Native-AOT-friendly logger that writes "[HH:mm:ss LVL] message" to stdout with no
///     reflection. Works with the [LoggerMessage] source generator.
/// </summary>
internal sealed class ConsoleLogger<T>(LogLevel minimumLevel) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= minimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[{DateTime.Now:HH:mm:ss} {Abbreviate(logLevel)}] {message}"));

        if (exception is not null)
        {
            Console.WriteLine(exception);
        }
    }

    private static string Abbreviate(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "LOG"
        };
    }
}
