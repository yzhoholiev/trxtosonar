namespace TrxToSonar.Logging;

internal static class VerbosityExtensions
{
    public static LogLevel ToLogLevel(this Verbosity verbosity)
    {
        return verbosity switch
        {
            Verbosity.Quiet => LogLevel.Error,
            Verbosity.Minimal => LogLevel.Warning,
            Verbosity.Normal => LogLevel.Information,
            Verbosity.Detailed => LogLevel.Debug,
            Verbosity.Diagnostic => LogLevel.Trace,
            _ => LogLevel.Information
        };
    }
}
