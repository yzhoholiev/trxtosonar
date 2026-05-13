using TrxToSonar;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using Xunit;
using File = TrxToSonar.Model.Sonar.File;

namespace TrxToSonarTest;

public class ExtensionsTests
{
    [Fact]
    public void BuildTestDefinitionLookup_WithMatchingId_ReturnsUnitTest()
    {
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest { Id = "test-123", Name = "TestMethod1" };
        trxDocument.TestDefinitions.Add(unitTest);

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        Assert.True(lookup.TryGetValue("test-123", out UnitTest? result));
        Assert.NotNull(result);
        Assert.Equal("TestMethod1", result.Name);
    }

    [Fact]
    public void BuildTestDefinitionLookup_WithNonMatchingId_ReturnsFalse()
    {
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest { Id = "test-123", Name = "TestMethod1" };
        trxDocument.TestDefinitions.Add(unitTest);

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        Assert.False(lookup.ContainsKey("test-456"));
    }

    [Fact]
    public void BuildTestDefinitionLookup_SkipsDefinitionsWithNullId()
    {
        var trxDocument = new TrxDocument();
        trxDocument.TestDefinitions.Add(new UnitTest { Id = null, Name = "Nameless" });
        trxDocument.TestDefinitions.Add(new UnitTest { Id = "test-1", Name = "Named" });

        Dictionary<string, UnitTest> lookup = trxDocument.BuildTestDefinitionLookup();

        Assert.Single(lookup);
        Assert.True(lookup.ContainsKey("test-1"));
    }

    [Fact]
    public void GetFile_WithMatchingPath_ReturnsFile()
    {
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        File? result = sonarDocument.GetFile("path/to/test.cs");

        Assert.NotNull(result);
        Assert.Equal("path/to/test.cs", result.Path);
    }

    [Fact]
    public void GetFile_WithNonMatchingPath_ReturnsNull()
    {
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        File? result = sonarDocument.GetFile("path/to/other.cs");

        Assert.Null(result);
    }
}
