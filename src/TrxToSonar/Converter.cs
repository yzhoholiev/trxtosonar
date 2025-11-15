using System.Diagnostics.CodeAnalysis;
using System.Xml;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonar;

internal sealed partial class Converter(ILogger<Converter> logger) : IConverter
{
    public bool Save(SonarDocument sonarDocument, string outputFilename)
    {
        return XmlParser<SonarDocument>.Save(sonarDocument, outputFilename);
    }

    public SonarDocument? Parse(string? solutionDirectory, bool useAbsolutePath)
    {
        if (string.IsNullOrEmpty(solutionDirectory) || !Directory.Exists(solutionDirectory))
        {
            LogDirectoryNotExists(solutionDirectory);
            return null;
        }

        IEnumerable<string> trxFiles = Directory.EnumerateFiles(
            solutionDirectory,
            "*.trx",
            new EnumerationOptions { RecurseSubdirectories = true });

        List<SonarDocument> sonarDocuments = [];
        foreach (string trxFile in trxFiles)
        {
            LogParsingFile(trxFile);

            try
            {
                TrxDocument? trxDocument = XmlParser<TrxDocument>.Deserialize(trxFile);
                if (trxDocument is null)
                {
                    LogTrxNotParsed(trxFile);
                    continue;
                }

                SonarDocument sonarDocument = Convert(trxDocument, solutionDirectory, useAbsolutePath);
                sonarDocuments.Add(sonarDocument);
            }
            catch (TrxToSonarException ex) when (ex.InnerException is XmlException)
            {
                LogInvalidXmlFormat(ex, trxFile);
            }
            catch (Exception ex)
            {
                LogParsingFailed(ex, trxFile, ex.Message);
                return null;
            }
        }

        // Merge
        return Merge(sonarDocuments);
    }

    [LoggerMessage(LogLevel.Error, "Directory {SolutionDirectory} does not exists!")]
    private partial void LogDirectoryNotExists(string? solutionDirectory);

    [LoggerMessage(LogLevel.Information, "Parsing: {TrxFileName}")]
    private partial void LogParsingFile(string trxFileName);

    [LoggerMessage(LogLevel.Warning, "TRX document {TrxFileName} wasn't parsed")]
    private partial void LogTrxNotParsed(string trxFileName);

    [LoggerMessage(LogLevel.Error, "Invalid XML format in TRX file {TrxFileName}")]
    private partial void LogInvalidXmlFormat(Exception exception, string trxFileName);

    [LoggerMessage(LogLevel.Error, "TRX document {TrxFileName} parsing failed. Error: {Error}")]
    private partial void LogParsingFailed(Exception exception, string trxFileName, string error);

    [LoggerMessage(LogLevel.Information, "Merge {FilesCount} file(s)")]
    private partial void LogMergeFiles(int filesCount);

    [LoggerMessage(LogLevel.Warning, "Unit test definition not found for test {TestName}")]
    private partial void LogUnitTestNotFound(string? testName);

    [LoggerMessage(LogLevel.Information, "Passed: {TestName}")]
    private partial void LogTestPassed(string? testName);

    [LoggerMessage(LogLevel.Information, "Skipped: {TestName}")]
    private partial void LogTestSkipped(string? testName);

    [LoggerMessage(LogLevel.Information, "Failure: {TestName}")]
    private partial void LogTestFailure(string? testName);

    [LoggerMessage(LogLevel.Error, "Failed to get test file for test {TestName}. Error: {Error}")]
    private partial void LogGetTestFileFailed(Exception exception, string? testName, string error);

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

    private SonarDocument Convert(TrxDocument trxDocument, string solutionDirectory, bool useAbsolutePath)
    {
        var sonarDocument = new SonarDocument();

        foreach (UnitTestResult trxResult in trxDocument.Results)
        {
            UnitTest? unitTest = trxResult.GetUnitTest(trxDocument);

            if (unitTest is null)
            {
                LogUnitTestNotFound(trxResult.TestName);
                continue;
            }

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
                LogTestPassed(trxResult.TestName);
                break;
            case Outcome.NotExecuted:
                testCase.Skipped = new Skipped();
                LogTestSkipped(trxResult.TestName);
                break;
            default:
                testCase.Failure = new Failure(trxResult.Output?.ErrorInfo?.Message, trxResult.Output?.ErrorInfo?.StackTrace);
                LogTestFailure(trxResult.TestName);
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
            LogGetTestFileFailed(ex, testName, ex.Message);
            return false;
        }
    }
}
