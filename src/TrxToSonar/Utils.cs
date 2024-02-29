using System.Globalization;

namespace TrxToSonar;

public static class Utils
{
    public static long ToSonarDuration(string? trxDuration)
    {
        return TimeSpan.TryParse(trxDuration, CultureInfo.InvariantCulture, out TimeSpan result) ? (long) result.TotalMilliseconds : 0;
    }
}
