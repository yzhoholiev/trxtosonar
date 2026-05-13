using System.Globalization;
using TrxToSonar.Model.Trx;

namespace TrxToSonar;

internal sealed class TestFileResolver(string solutionDirectory, bool useAbsolutePath)
{
    private static readonly string[] SearchPatternFormats =
    [
        "{0}.cs",
        "{0}Test.cs",
        "{0}Tests.cs",
        "{0}.vb",
        "{0}Test.vb",
        "{0}Tests.vb",
        "{0}*"
    ];

    private static readonly string TestProjectSignature = Path.Combine("Tests", "bin");
    private static readonly int ProjectRootSuffixLength = "Tests".Length + 1;
    private static readonly string[] SourceFileExtensions = ["*.cs", "*.vb"];

    private readonly Dictionary<string, List<string>> _projectFiles = new(StringComparer.OrdinalIgnoreCase);

    public string Resolve(UnitTest? unitTest)
    {
        string? fullClassName = unitTest?.TestMethod?.ClassName;

        if (string.IsNullOrEmpty(fullClassName))
        {
            throw new TrxToSonarException("Class name was not provided");
        }

        string className = GetSimpleClassName(fullClassName);
        string projectDirectory = GetProjectDirectory(unitTest!.TestMethod!);

        string file = FindInProject(projectDirectory, className)
                      ?? throw new FileNotFoundException($"Cannot find file with class {className}. Check that file has the same name as the class.");

        return useAbsolutePath ? file : Path.GetRelativePath(solutionDirectory, file);
    }

    private static string GetSimpleClassName(string fullClassName)
    {
        int lastDotIndex = fullClassName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullClassName[(lastDotIndex + 1)..] : fullClassName;
    }

    private static string GetProjectDirectory(TestMethod testMethod)
    {
        int indexOfSignature = testMethod.CodeBase.IndexOf(TestProjectSignature, StringComparison.OrdinalIgnoreCase);
        if (indexOfSignature < 0)
        {
            throw new TrxToSonarException($"Could not find test project signature '{TestProjectSignature}' in code base path: {testMethod.CodeBase}");
        }

        return Path.GetFullPath(testMethod.CodeBase[..(indexOfSignature + ProjectRootSuffixLength)]);
    }

    private string? FindInProject(string projectDirectory, string className)
    {
        List<string> files = GetProjectFiles(projectDirectory);

        foreach (string pattern in SearchPatternFormats)
        {
            bool isPrefix = pattern == "{0}*";
            string target = string.Format(CultureInfo.InvariantCulture, pattern, className);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                bool hit = isPrefix
                    ? name.StartsWith(className, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(name, target, StringComparison.OrdinalIgnoreCase);

                if (hit)
                {
                    return file;
                }
            }
        }

        return null;
    }

    private List<string> GetProjectFiles(string projectDirectory)
    {
        if (_projectFiles.TryGetValue(projectDirectory, out List<string>? cached))
        {
            return cached;
        }

        var files = new List<string>();
        if (Directory.Exists(projectDirectory))
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true };
            foreach (string extension in SourceFileExtensions)
            {
                files.AddRange(Directory.EnumerateFiles(projectDirectory, extension, options));
            }
        }

        _projectFiles[projectDirectory] = files;
        return files;
    }
}
