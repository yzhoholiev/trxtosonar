using TrxToSonar.Sonar.Models;
using TrxToSonar.Trx.Models;

namespace TrxToSonar;

internal static class TestCaseFactory
{
    public static TestCase Create(UnitTestResult trxResult)
    {
        long duration = Utils.ToSonarDuration(trxResult.Duration);
        ErrorInfo? errorInfo = trxResult.Output?.ErrorInfo;
        string? testName = trxResult.TestName;

        return trxResult.Outcome switch
        {
            Outcome.Passed or Outcome.Completed => new TestCase(testName, duration),
            Outcome.NotExecuted or Outcome.Pending or Outcome.InProgress => new TestCase(testName, duration) { Skipped = new Skipped() },
            Outcome.Failed => new TestCase(testName, duration) { Failure = new Failure(errorInfo?.Message, errorInfo?.StackTrace) },
            _ => new TestCase(testName, duration) { Error = new Error(errorInfo?.Message, errorInfo?.StackTrace) }
        };
    }
}
