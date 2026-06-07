namespace TrxToSonar.Trx.Models;

public sealed record Output
{
    public string? StdOut { get; init; }

    public ErrorInfo? ErrorInfo { get; init; }
}
