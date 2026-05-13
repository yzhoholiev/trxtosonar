using TrxToSonar.Model.Sonar;

namespace TrxToSonar;

internal readonly record struct ConversionResult(
    SonarDocument? Document,
    int TrxFileCount,
    int Passed,
    int Skipped,
    int Failed,
    int Errored,
    int Unresolved)
{
    public int Total => Passed + Skipped + Failed + Errored;
}
