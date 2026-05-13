using System.Security;
using Microsoft.Extensions.Logging.Abstractions;
using TrxToSonar;
using TrxToSonar.Model.Sonar;
using Xunit;
using File = TrxToSonar.Model.Sonar.File;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class ConverterEndToEndTests
{
    [Fact]
    public void Parse_FullTrxFixture_MapsOutcomesToCorrectSonarElements()
    {
        // Arrange: build a fake solution with a test project containing one source file
        // and a TRX referencing four tests covering each outcome bucket.
        string solutionDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string projectDir = Path.Combine(solutionDir, "MyApp.Tests");
        string binDir = Path.Combine(projectDir, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(binDir);

        string sourcePath = Path.Combine(projectDir, "SampleTests.cs");
        IOFile.WriteAllText(sourcePath, "// sample");

        string trxPath = Path.Combine(solutionDir, "results.trx");
        IOFile.WriteAllText(trxPath, BuildTrx(binDir));

        try
        {
            var converter = new Converter(NullLogger<Converter>.Instance);

            // Act
            ConversionResult result = converter.Parse(solutionDir, false);

            // Assert
            Assert.NotNull(result.Document);
            File file = Assert.Single(result.Document.Files);
            Assert.Equal(Path.Combine("MyApp.Tests", "SampleTests.cs"), file.Path);

            Assert.Equal(4, file.TestCases.Count);
            Assert.Equal(1, result.Passed);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(1, result.Failed);
            Assert.Equal(1, result.Errored);
            Assert.Equal(0, result.Unresolved);
            Assert.Equal(1, result.TrxFileCount);

            TestCase passed = file.TestCases.Single(t => t.Name == "PassingTest");
            Assert.Null(passed.Skipped);
            Assert.Null(passed.Failure);
            Assert.Null(passed.Error);
            Assert.Equal(15, passed.Duration);

            TestCase failed = file.TestCases.Single(t => t.Name == "FailingTest");
            Assert.NotNull(failed.Failure);
            Assert.Null(failed.Error);
            Assert.Equal("Assertion failed", failed.Failure.Message);
            Assert.Contains("at FailingTest", failed.Failure.Value, StringComparison.Ordinal);

            TestCase skipped = file.TestCases.Single(t => t.Name == "SkippedTest");
            Assert.NotNull(skipped.Skipped);
            Assert.Null(skipped.Failure);
            Assert.Null(skipped.Error);

            TestCase errored = file.TestCases.Single(t => t.Name == "ErroredTest");
            Assert.NotNull(errored.Error);
            Assert.Null(errored.Failure);
            Assert.Equal("Exception thrown", errored.Error.Message);
        }
        finally
        {
            if (Directory.Exists(solutionDir))
            {
                Directory.Delete(solutionDir, true);
            }
        }
    }

    [Fact]
    public void Parse_SerializedOutput_ContainsBothErrorAndFailureElements()
    {
        // Round-trip the converted document through XmlParser to make sure the
        // bug-fix (Error vs Failure) actually shows up in the produced XML.
        string solutionDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string projectDir = Path.Combine(solutionDir, "MyApp.Tests");
        string binDir = Path.Combine(projectDir, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(binDir);
        IOFile.WriteAllText(Path.Combine(projectDir, "SampleTests.cs"), "// sample");
        IOFile.WriteAllText(Path.Combine(solutionDir, "results.trx"), BuildTrx(binDir));

        string outputPath = Path.Combine(solutionDir, "sonar.xml");

        try
        {
            var converter = new Converter(NullLogger<Converter>.Instance);
            ConversionResult result = converter.Parse(solutionDir, false);
            Assert.NotNull(result.Document);
            Assert.True(Converter.Save(result.Document, outputPath));

            string xml = IOFile.ReadAllText(outputPath);
            Assert.Contains("<failure message=\"Assertion failed\"", xml, StringComparison.Ordinal);
            Assert.Contains("<error message=\"Exception thrown\"", xml, StringComparison.Ordinal);
            Assert.Contains("<skipped", xml, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(solutionDir))
            {
                Directory.Delete(solutionDir, true);
            }
        }
    }

    private static string BuildTrx(string codeBase)
    {
        // Backslashes need escaping for the XML attribute on Windows paths.
        string codeBaseAttr = SecurityElement.Escape(codeBase);

        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                  <Results>
                    <UnitTestResult executionId="e1" testId="t1" testName="PassingTest"
                                    duration="00:00:00.0150000" startTime="2024-01-01T00:00:00Z" endTime="2024-01-01T00:00:00Z"
                                    outcome="Passed" />
                    <UnitTestResult executionId="e2" testId="t2" testName="FailingTest"
                                    duration="00:00:00.0200000" startTime="2024-01-01T00:00:00Z" endTime="2024-01-01T00:00:00Z"
                                    outcome="Failed">
                      <Output>
                        <ErrorInfo>
                          <Message>Assertion failed</Message>
                          <StackTrace>at FailingTest() in SampleTests.cs:line 10</StackTrace>
                        </ErrorInfo>
                      </Output>
                    </UnitTestResult>
                    <UnitTestResult executionId="e3" testId="t3" testName="SkippedTest"
                                    duration="00:00:00" startTime="2024-01-01T00:00:00Z" endTime="2024-01-01T00:00:00Z"
                                    outcome="NotExecuted" />
                    <UnitTestResult executionId="e4" testId="t4" testName="ErroredTest"
                                    duration="00:00:00.0050000" startTime="2024-01-01T00:00:00Z" endTime="2024-01-01T00:00:00Z"
                                    outcome="Error">
                      <Output>
                        <ErrorInfo>
                          <Message>Exception thrown</Message>
                          <StackTrace>at ErroredTest() in SampleTests.cs:line 20</StackTrace>
                        </ErrorInfo>
                      </Output>
                    </UnitTestResult>
                  </Results>
                  <TestDefinitions>
                    <UnitTest id="t1" name="PassingTest">
                      <TestMethod codeBase="{codeBaseAttr}" className="MyApp.Tests.SampleTests" name="PassingTest" />
                    </UnitTest>
                    <UnitTest id="t2" name="FailingTest">
                      <TestMethod codeBase="{codeBaseAttr}" className="MyApp.Tests.SampleTests" name="FailingTest" />
                    </UnitTest>
                    <UnitTest id="t3" name="SkippedTest">
                      <TestMethod codeBase="{codeBaseAttr}" className="MyApp.Tests.SampleTests" name="SkippedTest" />
                    </UnitTest>
                    <UnitTest id="t4" name="ErroredTest">
                      <TestMethod codeBase="{codeBaseAttr}" className="MyApp.Tests.SampleTests" name="ErroredTest" />
                    </UnitTest>
                  </TestDefinitions>
                </TestRun>
                """;
    }
}
