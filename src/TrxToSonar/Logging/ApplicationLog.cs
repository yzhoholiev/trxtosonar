namespace TrxToSonar.Logging;

internal static partial class ApplicationLog
{
    [LoggerMessage(
        LogLevel.Information,
        "Processed {TrxFileCount} TRX file(s): {Total} test(s) — {Passed} passed, {Skipped} skipped, {Failed} failed, {Errored} errored, {Unresolved} unresolved")]
    public static partial void Summary(
        this ILogger logger,
        int trxFileCount,
        int total,
        int passed,
        int skipped,
        int failed,
        int errored,
        int unresolved);

    [LoggerMessage(LogLevel.Error, "An error occurred while processing TRX files")]
    public static partial void ProcessingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Application terminated unexpectedly")]
    public static partial void TerminatedUnexpectedly(this ILogger logger, Exception exception);
}
