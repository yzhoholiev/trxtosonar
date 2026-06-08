using TrxToSonar;
using TrxToSonar.Sonar.Models;
using TrxToSonar.Trx.Models;
using File = TrxToSonar.Sonar.Models.File;

namespace TrxToSonarTest;

public class ExtensionsTests
{
    [Test]
    public async Task BuildTestDefinitionLookup_WithMatchingId_ReturnsUnitTest()
    {
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest(Id: "test-123", Name: "TestMethod1");
        trxDocument.TestDefinitions.Add(unitTest);

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        await Assert.That(lookup.TryGetValue("test-123", out UnitTest? result)).IsTrue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("TestMethod1");
    }

    [Test]
    public async Task BuildTestDefinitionLookup_WithNonMatchingId_ReturnsFalse()
    {
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest(Id: "test-123", Name: "TestMethod1");
        trxDocument.TestDefinitions.Add(unitTest);

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        await Assert.That(lookup.ContainsKey("test-456")).IsFalse();
    }

    [Test]
    public async Task BuildTestDefinitionLookup_SkipsDefinitionsWithNullId()
    {
        var trxDocument = new TrxDocument();
        trxDocument.TestDefinitions.Add(new UnitTest(Id: null, Name: "Nameless"));
        trxDocument.TestDefinitions.Add(new UnitTest(Id: "test-1", Name: "Named"));

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        await Assert.That(lookup).HasSingleItem();
        await Assert.That(lookup.ContainsKey("test-1")).IsTrue();
    }

    [Test]
    public async Task GetFile_WithMatchingPath_ReturnsFile()
    {
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        File? result = sonarDocument.GetFile("path/to/test.cs");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Path).IsEqualTo("path/to/test.cs");
    }

    [Test]
    public async Task GetFile_WithNonMatchingPath_ReturnsNull()
    {
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        File? result = sonarDocument.GetFile("path/to/other.cs");

        await Assert.That(result).IsNull();
    }
}
