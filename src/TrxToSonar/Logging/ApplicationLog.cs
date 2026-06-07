namespace TrxToSonar.Logging;

internal static partial class ApplicationLog
{
    [LoggerMessage(LogLevel.Error, "An error occurred while processing TRX files")]
    public static partial void ProcessingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Application terminated unexpectedly")]
    public static partial void TerminatedUnexpectedly(this ILogger logger, Exception exception);
}
