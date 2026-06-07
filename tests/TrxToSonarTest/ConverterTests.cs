using Microsoft.Extensions.Logging.Abstractions;
using TrxToSonar;
using TrxToSonar.Sonar.Models;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class ConverterTests
{
    private readonly Converter _converter = new(NullLogger<Converter>.Instance);

    [Test]
    public async Task Parse_WithNullDirectory_ReturnsNullDocument()
    {
        ConversionResult result = _converter.Parse(null, false);

        await Assert.That(result.Document).IsNull();
        await Assert.That(result.TrxFileCount).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_WithEmptyDirectory_ReturnsNullDocument()
    {
        ConversionResult result = _converter.Parse(string.Empty, false);

        await Assert.That(result.Document).IsNull();
    }

    [Test]
    public async Task Parse_WithNonExistentDirectory_ReturnsNullDocument()
    {
        string nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        ConversionResult result = _converter.Parse(nonExistentDir, false);

        await Assert.That(result.Document).IsNull();
    }

    [Test]
    public async Task Parse_WithDirectoryWithoutTrxFiles_ReturnsEmptySonarDocument()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            ConversionResult result = _converter.Parse(tempDir, false);

            await Assert.That(result.Document).IsNotNull();
            await Assert.That(result.Document!.Files).IsEmpty();
            await Assert.That(result.TrxFileCount).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public async Task Save_WithValidDocument_ReturnsTrue()
    {
        var sonarDocument = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            bool result = Converter.Save(sonarDocument, outputFile);

            await Assert.That(result).IsTrue();
            await Assert.That(IOFile.Exists(outputFile)).IsTrue();
        }
        finally
        {
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }

    [Test]
    public async Task Save_OverwritesExistingFile()
    {
        var sonarDocument = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            IOFile.WriteAllText(outputFile, "initial content");

            bool result = Converter.Save(sonarDocument, outputFile);

            await Assert.That(result).IsTrue();
            await Assert.That(IOFile.Exists(outputFile)).IsTrue();
            string content = IOFile.ReadAllText(outputFile);
            await Assert.That(content).Contains("testExecutions");
            await Assert.That(content).DoesNotContain("initial content");
        }
        finally
        {
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }

    [Test]
    public async Task Save_CreatesDirectoryIfNotExists()
    {
        var sonarDocument = new SonarDocument();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string outputFile = Path.Combine(tempDir, "output.xml");

        try
        {
            bool result = Converter.Save(sonarDocument, outputFile);

            await Assert.That(result).IsTrue();
            await Assert.That(Directory.Exists(tempDir)).IsTrue();
            await Assert.That(IOFile.Exists(outputFile)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public async Task Parse_WithMalformedTrxFile_SkipsFileAndDoesNotAbort()
    {
        // The directory's only TRX file is not well-formed XML.
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        IOFile.WriteAllText(Path.Combine(tempDir, "broken.trx"), "<TestRun><Results>");

        try
        {
            ConversionResult result = _converter.Parse(tempDir, false);

            // The bad file is counted but skipped; the run still produces a document.
            await Assert.That(result.Document).IsNotNull();
            await Assert.That(result.Document!.Files).IsEmpty();
            await Assert.That(result.TrxFileCount).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
