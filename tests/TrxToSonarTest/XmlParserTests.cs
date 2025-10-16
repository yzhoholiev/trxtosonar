using TrxToSonar;
using TrxToSonar.Model.Sonar;
using Xunit;
using File = TrxToSonar.Model.Sonar.File;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class XmlParserTests
{
    [Fact]
    public void Serialize_WithValidSonarDocument_ReturnsXmlString()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        var document = new SonarDocument();
        document.Files.Add(new File("test.cs"));
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            // Act
            bool result = parser.Save(document, outputFile);

            // Assert
            Assert.True(result);
            string content = IOFile.ReadAllText(outputFile);
            Assert.Contains("<testExecutions", content, StringComparison.Ordinal);
            Assert.Contains("version=\"1\"", content, StringComparison.Ordinal);
            Assert.Contains("<file path=\"test.cs\"", content, StringComparison.Ordinal);
        }
        finally
        {
            // Cleanup
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Deserialize_WithValidXmlFile_ReturnsSonarDocument()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        string xmlContent = """
                            <testExecutions version="1">
                              <file path="test.cs">
                                <testCase name="TestMethod1" duration="100" />
                              </file>
                            </testExecutions>
                            """;
        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
        IOFile.WriteAllText(tempFile, xmlContent);

        try
        {
            // Act
            SonarDocument? result = parser.Deserialize(tempFile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Version);
            Assert.Single(result.Files);
            Assert.Equal("test.cs", result.Files[0].Path);
        }
        finally
        {
            // Cleanup
            if (IOFile.Exists(tempFile))
            {
                IOFile.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Save_DeletesExistingFile()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        var document = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
        IOFile.WriteAllText(outputFile, "old content");

        try
        {
            // Act
            bool result = parser.Save(document, outputFile);

            // Assert
            Assert.True(result);
            string content = IOFile.ReadAllText(outputFile);
            Assert.DoesNotContain("old content", content, StringComparison.Ordinal);
        }
        finally
        {
            // Cleanup
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        var document = new SonarDocument();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string outputFile = Path.Combine(tempDir, "output.xml");

        try
        {
            // Act
            bool result = parser.Save(document, outputFile);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(tempDir));
            Assert.True(IOFile.Exists(outputFile));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Serialize_OmitsXmlDeclaration()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        var document = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            // Act
            parser.Save(document, outputFile);
            string content = IOFile.ReadAllText(outputFile);

            // Assert
            Assert.DoesNotContain("<?xml", content, StringComparison.Ordinal);
        }
        finally
        {
            // Cleanup
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Serialize_HasIndentation()
    {
        // Arrange
        var parser = new XmlParser<SonarDocument>();
        var document = new SonarDocument();
        document.Files.Add(new File("test.cs"));
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            // Act
            parser.Save(document, outputFile);
            string content = IOFile.ReadAllText(outputFile);

            // Assert
            Assert.Contains(Environment.NewLine, content, StringComparison.Ordinal);
            Assert.Contains("  <file", content, StringComparison.Ordinal); // Check for indentation
        }
        finally
        {
            // Cleanup
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }
}
