using System.Diagnostics.CodeAnalysis;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonar;

public sealed class Converter(ILogger<Converter> logger) : IConverter
{
    private readonly XmlParser<SonarDocument> _sonarParser = new();
    private readonly XmlParser<TrxDocument> _trxParser = new();

    public bool Save(SonarDocument sonarDocument, string outputFilename)
    {
        return _sonarParser.Save(sonarDocument, outputFilename);
    }

    public SonarDocument? Parse(string? solutionDirectory, bool useAbsolutePath)
    {
        if (string.IsNullOrEmpty(solutionDirectory) || !Directory.Exists(solutionDirectory))
        {
            logger.LogError("Directory {SolutionDirectory} does not exists!", solutionDirectory);
            return null;
        }

        IEnumerable<string> trxFiles = Directory.EnumerateFiles(solutionDirectory, "*.trx", SearchOption.AllDirectories);

        var sonarDocuments = new List<SonarDocument>();
        foreach (string trxFile in trxFiles)
        {
            logger.LogInformation("Parsing: {TrxFileName}", trxFile);
            TrxDocument? trxDocument = _trxParser.Deserialize(trxFile);
            if (trxDocument is null)
            {
                logger.LogWarning("TRX document {TrxFileName} wasn't parsed", trxFile);
                continue;
            }

            try
            {
                SonarDocument sonarDocument = Convert(trxDocument, solutionDirectory, useAbsolutePath);
                sonarDocuments.Add(sonarDocument);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TRX document {TrxFileName} parsing failed. Error: {Error}", trxFile, ex.Message);
                return null;
            }
        }

        // Merge
        return Merge(sonarDocuments);
    }

    private SonarDocument Merge(List<SonarDocument> sonarDocuments)
    {
        logger.LogInformation("Merge {FilesCount} file(s)", sonarDocuments.Count);
        if (sonarDocuments.Count == 1)
        {
            return sonarDocuments[0];
        }

        var result = new SonarDocument();
        result.Files.AddRange(sonarDocuments.SelectMany(d => d.Files));

        return result;
    }

    private SonarDocument Convert(TrxDocument trxDocument, string solutionDirectory, bool useAbsolutePath)
    {
        var sonarDocument = new SonarDocument();

        foreach (UnitTestResult trxResult in trxDocument.Results)
        {
            UnitTest? unitTest = trxResult.GetUnitTest(trxDocument);

            if (!TryGetTestFile(unitTest, solutionDirectory, useAbsolutePath, trxResult.TestName, out string? testFile))
            {
                continue;
            }

            File file = GetOrAddFile(sonarDocument, testFile);
            TestCase testCase = CreateTestCase(trxResult);
            file.TestCases.Add(testCase);
        }

        return sonarDocument;
    }

    private TestCase CreateTestCase(UnitTestResult trxResult)
    {
        var testCase = new TestCase(trxResult.TestName, Utils.ToSonarDuration(trxResult.Duration));

        switch (trxResult.Outcome)
        {
            case Outcome.Passed:
                logger.LogInformation("Passed: {TestName}", trxResult.TestName);
                break;
            case Outcome.NotExecuted:
                testCase.Skipped = new Skipped();
                logger.LogInformation("Skipped: {TestName}", trxResult.TestName);
                break;
            default:
                testCase.Failure = new Failure(trxResult.Output?.ErrorInfo?.Message, trxResult.Output?.ErrorInfo?.StackTrace);
                logger.LogInformation("Failure: {TestName}", trxResult.TestName);
                break;
        }

        return testCase;
    }

    private static File GetOrAddFile(SonarDocument sonarDocument, string testFile)
    {
        File? file = sonarDocument.GetFile(testFile);

        if (file is not null)
        {
            return file;
        }

        file = new File(testFile);
        sonarDocument.Files.Add(file);

        return file;
    }

    private bool TryGetTestFile(UnitTest? unitTest, string solutionDirectory, bool useAbsolutePath, string? testName, [NotNullWhen(true)] out string? testFile)
    {
        testFile = null;
        try
        {
            testFile = unitTest.GetTestFile(solutionDirectory, useAbsolutePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get test file for test {TestName}. Error: {Error}", testName, ex.Message);
            return false;
        }
    }
}
