using System.Globalization;
using System.Reflection;

namespace TrxToSonar;

internal static class ConsoleOutput
{
    public static void WriteLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? assembly.GetName().Version?.ToString()
                         ?? "Unknown";

        Console.WriteLine($"trx2sonar {version}");
    }

    public static void WriteSummary(ConversionResult result)
    {
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Processed {result.TrxFileCount} TRX file(s): {result.Total} test(s) — {result.Passed} passed, {result.Skipped} skipped, {result.Failed} failed, {result.Errored} errored, {result.Unresolved} unresolved"));
    }
}
