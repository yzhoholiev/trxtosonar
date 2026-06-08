namespace TrxToSonar.Trx.Models;

public sealed record UnitTest(
    string? Name = null,
    string? Storage = null,
    string? Id = null,
    Execution? Execution = null,
    TestMethod? TestMethod = null);
