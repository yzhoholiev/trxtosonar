namespace TrxToSonar.Sonar.Models;

public sealed record TestCase(string? Name, long Duration)
{
    public Error? Error { get; init; }

    public Skipped? Skipped { get; init; }

    public Failure? Failure { get; init; }
}
