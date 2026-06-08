using TrxToSonar.Trx.Models;

namespace TrxToSonar;

internal sealed class TestDefinitionResolver
{
    private readonly Dictionary<string, UnitTest> _definitions;

    public TestDefinitionResolver(TrxDocument trxDocument)
    {
        _definitions = new Dictionary<string, UnitTest>(trxDocument.TestDefinitions.Count, StringComparer.Ordinal);
        foreach (UnitTest test in trxDocument.TestDefinitions)
        {
            if (test.Id is not null)
            {
                _definitions[test.Id] = test;
            }
        }
    }

    public UnitTest? Resolve(string? testId)
    {
        return testId is not null && _definitions.TryGetValue(testId, out UnitTest? unitTest)
            ? unitTest
            : null;
    }
}
