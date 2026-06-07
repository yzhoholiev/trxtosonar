namespace TrxToSonar.Sonar.Models;

public sealed record Skipped
{
    public string Message { get; init; } = "Skipped";

    public string? Value { get; init; }
}
