using TrxToSonar;
using TrxToSonar.Trx.Models;

namespace TrxToSonarTest;

public sealed class TestDefinitionResolverTests
{
    [Test]
    public async Task Resolve_WithMatchingId_ReturnsDefinition()
    {
        var trxDocument = new TrxDocument();
        trxDocument.TestDefinitions.Add(new UnitTest(Id: "t1", Name: "First"));

        var resolver = new TestDefinitionResolver(trxDocument);

        UnitTest? result = resolver.Resolve("t1");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("First");
    }

    [Test]
    public async Task Resolve_WithUnknownId_ReturnsNull()
    {
        var trxDocument = new TrxDocument();
        trxDocument.TestDefinitions.Add(new UnitTest(Id: "t1", Name: "First"));

        var resolver = new TestDefinitionResolver(trxDocument);

        await Assert.That(resolver.Resolve("missing")).IsNull();
    }

    [Test]
    public async Task Resolve_WithNullId_ReturnsNull()
    {
        var resolver = new TestDefinitionResolver(new TrxDocument());

        await Assert.That(resolver.Resolve(null)).IsNull();
    }

    [Test]
    public async Task Resolve_IgnoresDefinitionsWithNullId()
    {
        var trxDocument = new TrxDocument();
        trxDocument.TestDefinitions.Add(new UnitTest(Id: null, Name: "Nameless"));
        trxDocument.TestDefinitions.Add(new UnitTest(Id: "t1", Name: "Named"));

        var resolver = new TestDefinitionResolver(trxDocument);

        await Assert.That(resolver.Resolve("t1")).IsNotNull();
    }
}
