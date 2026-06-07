namespace TrxToSonar.Trx.Models;

public sealed record TestMethod
{
    public string CodeBase { get; init; } = null!;

    public string? ClassName { get; init; }

    public string? Name { get; init; }
}
