using System.Diagnostics.CodeAnalysis;
using System.Xml;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonar;

internal sealed partial class Converter(ILogger<Converter> logger)
{
    public static bool Save(SonarDocument sonarDocument, string outputFilename)
    {
        return XmlParser<SonarDocument>.Save(sonarDocument, outputFilename);
    }

    public ConversionResult Parse(string? solutionDirectory, bool useAbsolutePath)
    {
        if (string.IsNullOrEmpty(solutionDirectory) || !Directory.Exists(solutionDirectory))
        {
            LogDirectoryNotExists(solutionDirectory);
            return new ConversionResult(null, 0, 0, 0, 0, 0, 0);
        }

        IEnumerable<string> trxFiles = Directory.EnumerateFiles(
            solutionDirectory,
            "*.trx",
            new EnumerationOptions { RecurseSubdirectories = true });

        var resolver = new TestFileResolver(solutionDirectory, useAbsolutePath);
        List<SonarDocument> sonarDocuments = [];
        int trxCount = 0;
        int unresolved = 0;
        foreach (string trxFile in trxFiles)
        {
            LogParsingFile(trxFile);
            trxCount++;

            try
            {
                TrxDocument? trxDocument = XmlParser<TrxDocument>.Deserialize(trxFile);
                if (trxDocument is null)
                {
                    LogTrxNotParsed(trxFile);
                    continue;
                }

                (SonarDocument doc, int convertUnresolved) = Convert(trxDocument, resolver);
                sonarDocuments.Add(doc);
                unresolved += convertUnresolved;
            }
            catch (TrxToSonarException ex) when (ex.InnerException is XmlException)
            {
                LogInvalidXmlFormat(ex, trxFile);
            }
            catch (Exception ex)
            {
                LogParsingFailed(ex, trxFile);
                return new ConversionResult(null, trxCount, 0, 0, 0, 0, unresolved);
            }
        }

        SonarDocument merged = Merge(sonarDocuments);
        (int passed, int skipped, int failed, int errored) = CountOutcomes(merged);
        return new ConversionResult(merged, trxCount, passed, skipped, failed, errored, unresolved);
    }

    [LoggerMessage(LogLevel.Error, "Directory does not exist: {SolutionDirectory}")]
    private partial void LogDirectoryNotExists(string? solutionDirectory);

    [LoggerMessage(LogLevel.Information, "Parsing: {TrxFileName}")]
    private partial void LogParsingFile(string trxFileName);

    [LoggerMessage(LogLevel.Warning, "TRX document {TrxFileName} wasn't parsed")]
    private partial void LogTrxNotParsed(string trxFileName);

    [LoggerMessage(LogLevel.Error, "Invalid XML format in TRX file {TrxFileName}")]
    private partial void LogInvalidXmlFormat(Exception exception, string trxFileName);

    [LoggerMessage(LogLevel.Error, "TRX document {TrxFileName} parsing failed")]
    private partial void LogParsingFailed(Exception exception, string trxFileName);

    [LoggerMessage(LogLevel.Information, "Merging {FileCount} TRX result document(s)")]
    private partial void LogMergeFiles(int fileCount);

    [LoggerMessage(LogLevel.Warning, "Unit test definition not found for test {TestName}")]
    private partial void LogUnitTestNotFound(string? testName);

    [LoggerMessage(LogLevel.Debug, "Passed: {TestName}")]
    private partial void LogTestPassed(string? testName);

    [LoggerMessage(LogLevel.Debug, "Skipped: {TestName}")]
    private partial void LogTestSkipped(string? testName);

    [LoggerMessage(LogLevel.Debug, "Failed: {TestName}")]
    private partial void LogTestFailed(string? testName);

    [LoggerMessage(LogLevel.Debug, "Errored: {TestName}")]
    private partial void LogTestErrored(string? testName);

    [LoggerMessage(LogLevel.Error, "Failed to get test file for test {TestName}")]
    private partial void LogGetTestFileFailed(Exception exception, string? testName);

    private SonarDocument Merge(List<SonarDocument> sonarDocuments)
    {
        LogMergeFiles(sonarDocuments.Count);

        if (sonarDocuments.Count == 1)
        {
            return sonarDocuments[0];
        }

        var result = new SonarDocument();

        foreach (SonarDocument doc in sonarDocuments)
        {
            result.Files.AddRange(doc.Files);
        }

        return result;
    }

    private (SonarDocument document, int unresolved) Convert(TrxDocument trxDocument, TestFileResolver resolver)
    {
        var sonarDocument = new SonarDocument();
        Dictionary<string, UnitTest> testDefinitions = trxDocument.BuildTestDefinitionLookup();
        int unresolved = 0;

        foreach (UnitTestResult trxResult in trxDocument.Results)
        {
            if (trxResult.TestId is null || !testDefinitions.TryGetValue(trxResult.TestId, out UnitTest? unitTest))
            {
                LogUnitTestNotFound(trxResult.TestName);
                unresolved++;
                continue;
            }

            if (!TryResolveTestFile(resolver, unitTest, trxResult.TestName, out string? testFile))
            {
                unresolved++;
                continue;
            }

            File file = GetOrAddFile(sonarDocument, testFile);
            TestCase testCase = CreateTestCase(trxResult);
            file.TestCases.Add(testCase);
        }

        return (sonarDocument, unresolved);
    }

    private static (int passed, int skipped, int failed, int errored) CountOutcomes(SonarDocument document)
    {
        int passed = 0;
        int skipped = 0;
        int failed = 0;
        int errored = 0;

        foreach (File file in document.Files)
        {
            foreach (TestCase test in file.TestCases)
            {
                if (test.Skipped is not null)
                {
                    skipped++;
                }
                else if (test.Failure is not null)
                {
                    failed++;
                }
                else if (test.Error is not null)
                {
                    errored++;
                }
                else
                {
                    passed++;
                }
            }
        }

        return (passed, skipped, failed, errored);
    }

    private TestCase CreateTestCase(UnitTestResult trxResult)
    {
        var testCase = new TestCase(trxResult.TestName, Utils.ToSonarDuration(trxResult.Duration));

        switch (trxResult.Outcome)
        {
            case Outcome.Passed:
            case Outcome.Completed:
                LogTestPassed(trxResult.TestName);
                break;
            case Outcome.NotExecuted:
            case Outcome.Pending:
            case Outcome.InProgress:
                testCase.Skipped = new Skipped();
                LogTestSkipped(trxResult.TestName);
                break;
            case Outcome.Failed:
                testCase.Failure = new Failure(trxResult.Output?.ErrorInfo?.Message, trxResult.Output?.ErrorInfo?.StackTrace);
                LogTestFailed(trxResult.TestName);
                break;
            default:
                testCase.Error = new Error(trxResult.Output?.ErrorInfo?.Message, trxResult.Output?.ErrorInfo?.StackTrace);
                LogTestErrored(trxResult.TestName);
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

    private bool TryResolveTestFile(
        TestFileResolver resolver,
        UnitTest? unitTest,
        string? testName,
        [NotNullWhen(true)] out string? testFile)
    {
        testFile = null;
        try
        {
            testFile = resolver.Resolve(unitTest);
            return true;
        }
        catch (Exception ex)
        {
            LogGetTestFileFailed(ex, testName);
            return false;
        }
    }
}
