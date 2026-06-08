namespace TrxToSonar.Trx.Models;

public sealed record UnitTestResult(
    string? ExecutionId = null,
    string? TestId = null,
    string? TestName = null,
    string? Duration = null,
    DateTime StartTime = default,
    DateTime EndTime = default,
    Outcome Outcome = default,
    Output? Output = null);
