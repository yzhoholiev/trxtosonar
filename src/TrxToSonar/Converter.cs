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

            string testFile = unitTest.GetTestFile(solutionDirectory, useAbsolutePath);

            File? file = sonarDocument.GetFile(testFile);

            if (file is null)
            {
                file = new File(testFile);
                sonarDocument.Files.Add(file);
            }

            var testCase = new TestCase(trxResult.TestName, Utils.ToSonarDuration(trxResult.Duration));

            if (trxResult.Outcome != Outcome.Passed)
            {
                if (trxResult.Outcome == Outcome.NotExecuted)
                {
                    testCase.Skipped = new Skipped();
                    logger.LogInformation("Skipped: {TestName}", trxResult.TestName);
                }
                else
                {
                    testCase.Failure = new Failure(
                        trxResult.Output?.ErrorInfo?.Message,
                        trxResult.Output?.ErrorInfo?.StackTrace);

                    logger.LogInformation("Failure: {TestName}", trxResult.TestName);
                }
            }
            else
            {
                logger.LogInformation("Passed: {TestName}", trxResult.TestName);
            }

            file.TestCases.Add(testCase);
        }

        return sonarDocument;
    }
}
