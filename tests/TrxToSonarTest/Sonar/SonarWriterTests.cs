using TrxToSonar.Sonar;
using TrxToSonar.Sonar.Models;
using File = TrxToSonar.Sonar.Models.File;
using IOFile = System.IO.File;

namespace TrxToSonarTest.Sonar;

public class SonarWriterTests
{
    [Test]
    public async Task Write_WithValidSonarDocument_WritesExpectedElements()
    {
        var document = new SonarDocument();
        document.Files.Add(new File("test.cs"));
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            bool result = SonarWriter.Write(document, outputFile);

            await Assert.That(result).IsTrue();
            string content = IOFile.ReadAllText(outputFile);
            await Assert.That(content).Contains("<testExecutions");
            await Assert.That(content).Contains("version=\"1\"");
            await Assert.That(content).Contains("<file path=\"test.cs\"");
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
    public async Task Write_ProducesExpectedXmlForEveryOutcome()
    {
        // One file with a passed, failed, errored, and skipped test case.
        var document = new SonarDocument();
        var file = new File("Tests\\MyTests.cs");
        file.TestCases.Add(new TestCase("PassedTest", 5));
        file.TestCases.Add(
            new TestCase("FailedTest", 10)
            {
                Failure = new Failure("Assertion failed", "   at FailingTest() in MyTests.cs:line 10")
            });
        file.TestCases.Add(
            new TestCase("ErroredTest", 7)
            {
                Error = new Error("Exception thrown", "   at ErroredTest() in MyTests.cs:line 20")
            });
        file.TestCases.Add(new TestCase("SkippedTest", 0) { Skipped = new Skipped() });
        document.Files.Add(file);

        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        // The consumer (SonarQube) parses the XML, so line endings are insignificant —
        // both sides are normalized before comparison. This still pins element/attribute
        // order, indentation, self-closed empties, and null-attribute omission.
        string expected = """
                          <testExecutions version="1">
                            <file path="Tests\MyTests.cs">
                              <testCase name="PassedTest" duration="5" />
                              <testCase name="FailedTest" duration="10">
                                <failure message="Assertion failed">   at FailingTest() in MyTests.cs:line 10</failure>
                              </testCase>
                              <testCase name="ErroredTest" duration="7">
                                <error message="Exception thrown">   at ErroredTest() in MyTests.cs:line 20</error>
                              </testCase>
                              <testCase name="SkippedTest" duration="0">
                                <skipped message="Skipped" />
                              </testCase>
                            </file>
                          </testExecutions>
                          """;

        try
        {
            SonarWriter.Write(document, outputFile);

            string content = IOFile.ReadAllText(outputFile);
            await Assert.That(content.ReplaceLineEndings("\n")).IsEqualTo(expected.ReplaceLineEndings("\n"));
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
    public async Task Write_DeletesExistingFile()
    {
        var document = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
        IOFile.WriteAllText(outputFile, "old content");

        try
        {
            bool result = SonarWriter.Write(document, outputFile);

            await Assert.That(result).IsTrue();
            string content = IOFile.ReadAllText(outputFile);
            await Assert.That(content).DoesNotContain("old content");
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
    public async Task Write_CreatesDirectoryIfNotExists()
    {
        var document = new SonarDocument();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string outputFile = Path.Combine(tempDir, "output.xml");

        try
        {
            bool result = SonarWriter.Write(document, outputFile);

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
    public async Task Write_OmitsXmlDeclaration()
    {
        var document = new SonarDocument();
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            SonarWriter.Write(document, outputFile);
            string content = IOFile.ReadAllText(outputFile);

            await Assert.That(content).DoesNotContain("<?xml");
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
    public async Task Write_IndentsNestedElements()
    {
        var document = new SonarDocument();
        document.Files.Add(new File("test.cs"));
        string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

        try
        {
            SonarWriter.Write(document, outputFile);
            string content = IOFile.ReadAllText(outputFile);

            await Assert.That(content).Contains("  <file");
        }
        finally
        {
            if (IOFile.Exists(outputFile))
            {
                IOFile.Delete(outputFile);
            }
        }
    }
}
