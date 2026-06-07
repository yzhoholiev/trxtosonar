namespace TrxToSonar.Trx.Models;

public sealed record ErrorInfo
{
    public string? Message { get; init; }

    public string? StackTrace { get; init; }
}
