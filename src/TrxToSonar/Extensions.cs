using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonar;

internal static class Extensions
{
    public static Dictionary<string, UnitTest> BuildTestDefinitionLookup(this TrxDocument trxDocument)
    {
        var lookup = new Dictionary<string, UnitTest>(trxDocument.TestDefinitions.Count, StringComparer.Ordinal);
        foreach (UnitTest test in trxDocument.TestDefinitions)
        {
            if (test.Id is not null)
            {
                lookup[test.Id] = test;
            }
        }

        return lookup;
    }

    public static File? GetFile(this SonarDocument sonarDocument, string testFile)
    {
        return sonarDocument.Files.Find(x => x.Path == testFile);
    }
}
