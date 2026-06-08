namespace TrxToSonar.Trx.Models;

public sealed record UnitTestResult(
    string? ExecutionId,
    string? TestId,
    string? TestName,
    string? Duration,
    DateTime StartTime,
    DateTime EndTime,
    Outcome Outcome,
    Output? Output);
