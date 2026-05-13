using System.Globalization;

namespace TrxToSonar;

internal static class ConsoleOutput
{
    public static void WriteSummary(ConversionResult result)
    {
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Processed {result.TrxFileCount} TRX file(s): {result.Total} test(s) — {result.Passed} passed, {result.Skipped} skipped, {result.Failed} failed, {result.Errored} errored, {result.Unresolved} unresolved"));
    }
}
