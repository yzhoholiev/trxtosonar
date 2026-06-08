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

    [LoggerMessage(LogLevel.Error, "Failed to write output: {Reason}")]
    public static partial void SaveFailed(this ILogger logger, string reason);

    [LoggerMessage(LogLevel.Critical, "Application terminated unexpectedly: {Reason}")]
    public static partial void TerminatedUnexpectedly(this ILogger logger, string reason);
}
