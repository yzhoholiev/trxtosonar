namespace TrxToSonar.Sonar.Models;

public sealed class SonarDocument
{
    public int Version { get; set; } = 1;

    public List<File> Files { get; } = [];
}
