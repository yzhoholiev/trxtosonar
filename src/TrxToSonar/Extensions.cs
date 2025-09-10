using System.Globalization;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonar;

internal static class Extensions
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

    public static UnitTest? GetUnitTest(this UnitTestResult unitTestResult, TrxDocument trxDocument)
    {
        return trxDocument.TestDefinitions.Find(x => x.Id == unitTestResult.TestId);
    }

    public static File? GetFile(this SonarDocument sonarDocument, string testFile)
    {
        return sonarDocument.Files.Find(x => x.Path == testFile);
    }

    public static string GetTestFile(this UnitTest? unitTest, string solutionDirectory, bool useAbsolutePath)
    {
        string? fullClassName = unitTest?.TestMethod?.ClassName;

        if (string.IsNullOrEmpty(fullClassName))
        {
            throw new TrxToSonarException("Class name was not provided");
        }

        string className = GetClassName(fullClassName);

        int indexOfSignature = unitTest!.TestMethod!.CodeBase.IndexOf(TestProjectSignature, StringComparison.OrdinalIgnoreCase);
        string projectDirectory = unitTest.TestMethod.CodeBase[..(indexOfSignature + 6)];

        string? file = FindFileInDirectory(projectDirectory, className);

        if (string.IsNullOrEmpty(file))
        {
            throw new FileNotFoundException($"Cannot find file with class {className}. Check that file has the same name as the class.");
        }

        if (!useAbsolutePath)
        {
            file = file[(solutionDirectory.Length + 1)..];
        }

        return file;
    }

    private static string GetClassName(string fullClassName)
    {
        int lastDotIndex = fullClassName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullClassName[(lastDotIndex + 1)..] : fullClassName;
    }

    private static string? FindFileInDirectory(string projectDirectory, string className)
    {
        foreach (string pattern in SearchPatternFormats)
        {
            string searchPattern = string.Format(CultureInfo.InvariantCulture, pattern, className);
            string[] files = Directory.GetFiles(projectDirectory, searchPattern, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                return files[0];
            }
        }

        return null;
    }
}
