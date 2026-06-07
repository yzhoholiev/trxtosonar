namespace TrxToSonar.Trx.Models;

public sealed class TrxDocument
{
    public List<UnitTestResult> Results { get; } = [];

    public List<UnitTest> TestDefinitions { get; } = [];

    public ResultSummary? ResultSummary { get; set; }
}
