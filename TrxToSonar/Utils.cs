namespace TrxToSonar;

public static class Utils
{
    public static long ToSonarDuration(string? trxDuration)
    {
        return TimeSpan.TryParse(trxDuration, out TimeSpan result) ? (long) result.TotalMilliseconds : 0;
    }
}
