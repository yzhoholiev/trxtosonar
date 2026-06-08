using TrxToSonar.Sonar.Models;
using File = TrxToSonar.Sonar.Models.File;

namespace TrxToSonar;

internal readonly struct ConversionResult
{
    public ConversionResult(SonarDocument? document, int trxFileCount, int unresolved)
    {
        Document = document;
        TrxFileCount = trxFileCount;
        Unresolved = unresolved;

        int passed = 0;
        int skipped = 0;
        int failed = 0;
        int errored = 0;

        if (document is not null)
        {
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
        }

        Passed = passed;
        Skipped = skipped;
        Failed = failed;
        Errored = errored;
    }

    public SonarDocument? Document { get; }

    public int TrxFileCount { get; }

    public int Unresolved { get; }

    public int Passed { get; }

    public int Skipped { get; }

    public int Failed { get; }

    public int Errored { get; }

    public int Total => Passed + Skipped + Failed + Errored;
}
