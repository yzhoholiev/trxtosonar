namespace TrxToSonar.Sonar.Models;

public sealed class File(string? path)
{
    public string? Path { get; } = path;

    public List<TestCase> TestCases { get; } = [];
}
