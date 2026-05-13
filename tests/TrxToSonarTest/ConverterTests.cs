using Microsoft.Extensions.Logging.Abstractions;
using TrxToSonar;
using TrxToSonar.Model.Sonar;
using Xunit;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class ConverterTests
{
    private readonly Converter _converter = new(NullLogger<Converter>.Instance);

    [Fact]
    public void Parse_WithNullDirectory_ReturnsNullDocument()
    {
        ConversionResult result = _converter.Parse(null, false);

        Assert.Null(result.Document);
        Assert.Equal(0, result.TrxFileCount);
    }

    [Fact]
    public void Parse_WithEmptyDirectory_ReturnsNullDocument()
    {
        ConversionResult result = _converter.Parse(string.Empty, false);

        Assert.Null(result.Document);
    }

    [Fact]
    public void Parse_WithNonExistentDirectory_ReturnsNullDocument()
    {
        string nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        ConversionResult result = _converter.Parse(nonExistentDir, false);

        Assert.Null(result.Document);
    }

    [Fact]
    public void Parse_WithDirectoryWithoutTrxFiles_ReturnsEmptySonarDocument()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            ConversionResult result = _converter.Parse(tempDir, false);

            Assert.NotNull(result.Document);
            Assert.Empty(result.Document.Files);
            Assert.Equal(0, result.TrxFileCount);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Save_WithValidDocument_ReturnsTrue()
    {
        // Arrange
        var sonarDocument = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            // Act
            bool result = Converter.Save(sonarDocument, outputFile);

            // Assert
            Assert.True(result);
            Assert.True(IOFile.Exists(outputFile));
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
    public void Save_OverwritesExistingFile()
    {
        // Arrange
        var sonarDocument = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            // Create initial file
            IOFile.WriteAllText(outputFile, "initial content");

            // Act
            bool result = Converter.Save(sonarDocument, outputFile);

            // Assert
            Assert.True(result);
            Assert.True(IOFile.Exists(outputFile));
            string content = IOFile.ReadAllText(outputFile);
            Assert.Contains("testExecutions", content, StringComparison.Ordinal);
            Assert.DoesNotContain("initial content", content, StringComparison.Ordinal);
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
        var sonarDocument = new SonarDocument();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string outputFile = Path.Combine(tempDir, "output.xml");

        try
        {
            // Act
            bool result = Converter.Save(sonarDocument, outputFile);

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
}
