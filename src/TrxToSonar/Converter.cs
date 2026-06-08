using System.Diagnostics.CodeAnalysis;
using TrxToSonar.Sonar;
using TrxToSonar.Sonar.Models;
using TrxToSonar.Trx;
using TrxToSonar.Trx.Models;

namespace TrxToSonar;

internal sealed partial class Converter(ILogger<Converter> logger)
{
    public static Result Save(SonarDocument sonarDocument, string outputFilename)
    {
        return SonarWriter.Write(sonarDocument, outputFilename);
    }

    public ConversionResult Parse(DirectoryInfo solutionDirectory, bool useAbsolutePath)
    {
        if (!solutionDirectory.Exists)
        {
            LogDirectoryNotExists(solutionDirectory.FullName);
            return new ConversionResult(null, 0, 0);
        }

        IEnumerable<FileInfo> trxFiles = solutionDirectory.EnumerateFiles(
            "*.trx",
            new EnumerationOptions { RecurseSubdirectories = true });

        var resolver = new TestFileResolver(solutionDirectory.FullName, useAbsolutePath);
        var document = new SonarDocument();
        int trxCount = 0;
        int unresolved = 0;
        foreach (FileInfo trxFile in trxFiles)
        {
            LogParsingFile(trxFile.FullName);
            trxCount++;

            TrxDocument? trxDocument = TrxReader.Read(trxFile);
            if (trxDocument is null)
            {
                LogTrxNotParsed(trxFile.FullName);
                continue;
            }

            unresolved += Convert(trxDocument, resolver, document);
        }

        return new ConversionResult(document, trxCount, unresolved);
    }

    [LoggerMessage(LogLevel.Error, "Directory does not exist: {SolutionDirectory}")]
    private partial void LogDirectoryNotExists(string? solutionDirectory);

    [LoggerMessage(LogLevel.Information, "Parsing: {TrxFileName}")]
    private partial void LogParsingFile(string trxFileName);

    [LoggerMessage(LogLevel.Warning, "TRX file {TrxFileName} could not be parsed and was skipped")]
    private partial void LogTrxNotParsed(string trxFileName);

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

    private int Convert(TrxDocument trxDocument, TestFileResolver resolver, SonarDocument document)
    {
        var definitions = new TestDefinitionResolver(trxDocument);
        int unresolved = 0;

        foreach (UnitTestResult trxResult in trxDocument.Results)
        {
            UnitTest? unitTest = definitions.Resolve(trxResult.TestId);
            if (unitTest is null)
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

            TestCase testCase = TestCaseFactory.Create(trxResult);
            LogOutcome(testCase, trxResult.TestName);
            document.AddTestCase(testFile, testCase);
        }

        return unresolved;
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
