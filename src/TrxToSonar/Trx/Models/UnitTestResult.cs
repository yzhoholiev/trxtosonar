namespace TrxToSonar.Trx.Models;

public sealed record UnitTestResult
{
    public string? ExecutionId { get; init; }

    public string? TestId { get; init; }

    public string? TestName { get; init; }

    public string? Duration { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    public Outcome Outcome { get; init; }

    public Output? Output { get; init; }
}
