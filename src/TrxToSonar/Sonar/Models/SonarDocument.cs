namespace TrxToSonar.Sonar.Models;

public sealed class SonarDocument
{
    private readonly Dictionary<string, File> _filesByPath = new(StringComparer.Ordinal);

    public int Version { get; set; } = 1;

    public List<File> Files { get; } = [];

    public void AddTestCase(string filePath, TestCase testCase)
    {
        if (!_filesByPath.TryGetValue(filePath, out File? file))
        {
            file = new File(filePath);
            _filesByPath[filePath] = file;
            Files.Add(file);
        }

        file.TestCases.Add(testCase);
    }
}
