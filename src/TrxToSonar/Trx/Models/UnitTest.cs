namespace TrxToSonar.Trx.Models;

public sealed record UnitTest
{
    public string? Name { get; init; }

    public string? Storage { get; init; }

    public string? Id { get; init; }

    public Execution? Execution { get; init; }

    public TestMethod? TestMethod { get; init; }
}
