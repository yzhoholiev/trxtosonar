using System.Diagnostics.CodeAnalysis;
using TrxToSonar.Sonar;
using TrxToSonar.Sonar.Models;
using TrxToSonar.Trx;
using TrxToSonar.Trx.Models;
using File = TrxToSonar.Sonar.Models.File;

namespace TrxToSonar;

internal sealed partial class Converter(ILogger<Converter> logger)
{
    public static Result Save(SonarDocument sonarDocument, string outputFilename)
    {
        return SonarWriter.Write(sonarDocument, outputFilename);
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

            TrxDocument? trxDocument = TrxReader.Read(trxFile);
            if (trxDocument is null)
            {
                LogTrxNotParsed(trxFile);
                continue;
            }

            (SonarDocument doc, int convertUnresolved) = Convert(trxDocument, resolver);
            sonarDocuments.Add(doc);
            unresolved += convertUnresolved;
        }

        SonarDocument merged = Merge(sonarDocuments);
        (int passed, int skipped, int failed, int errored) = CountOutcomes(merged);
        return new ConversionResult(merged, trxCount, passed, skipped, failed, errored, unresolved);
    }

    [LoggerMessage(LogLevel.Error, "Directory does not exist: {SolutionDirectory}")]
    private partial void LogDirectoryNotExists(string? solutionDirectory);

    [LoggerMessage(LogLevel.Information, "Parsing: {TrxFileName}")]
    private partial void LogParsingFile(string trxFileName);

    [LoggerMessage(LogLevel.Warning, "TRX file {TrxFileName} could not be parsed and was skipped")]
    private partial void LogTrxNotParsed(string trxFileName);

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

    [LoggerMessage(LogLevel.Error, "Failed to resolve test file for {TestName}: {Reason}")]
    private partial void LogResolveFailed(string? testName, string reason);

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
        long duration = Utils.ToSonarDuration(trxResult.Duration);
        ErrorInfo? errorInfo = trxResult.Output?.ErrorInfo;
        string? testName = trxResult.TestName;

        TestCase testCase = trxResult.Outcome switch
        {
            Outcome.Passed or Outcome.Completed => new TestCase(testName, duration),
            Outcome.NotExecuted or Outcome.Pending or Outcome.InProgress => new TestCase(testName, duration) { Skipped = new Skipped() },
            Outcome.Failed => new TestCase(testName, duration) { Failure = new Failure(errorInfo?.Message, errorInfo?.StackTrace) },
            _ => new TestCase(testName, duration) { Error = new Error(errorInfo?.Message, errorInfo?.StackTrace) }
        };

        LogOutcome(testCase, testName);
        return testCase;
    }

    private void LogOutcome(TestCase testCase, string? testName)
    {
        if (testCase.Skipped is not null)
        {
            LogTestSkipped(testName);
        }
        else if (testCase.Failure is not null)
        {
            LogTestFailed(testName);
        }
        else if (testCase.Error is not null)
        {
            LogTestErrored(testName);
        }
        else
        {
            LogTestPassed(testName);
        }
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
        Result<string> result = resolver.Resolve(unitTest);
        if (result.IsSuccess)
        {
            testFile = result.Value!;
            return true;
        }

        LogResolveFailed(testName, result.Error!);
        testFile = null;
        return false;
    }
}
