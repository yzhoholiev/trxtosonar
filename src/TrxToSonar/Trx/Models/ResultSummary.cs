namespace TrxToSonar.Trx.Models;

public sealed record ResultSummary
{
    public string? Outcome { get; init; }

    public Counters? Counters { get; init; }

    public Output? Output { get; init; }
}
