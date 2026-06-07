using System.Security;
using Microsoft.Extensions.Logging.Abstractions;
using TrxToSonar;
using TrxToSonar.Sonar.Models;
using File = TrxToSonar.Sonar.Models.File;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class ConverterEndToEndTests
{
    [Test]
    public async Task Parse_FullTrxFixture_MapsOutcomesToCorrectSonarElements()
    {
        // Build a fake solution with a test project containing one source file and a TRX
        // referencing four tests covering each outcome bucket.
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

            ConversionResult result = converter.Parse(solutionDir, false);

            await Assert.That(result.Document).IsNotNull();
            File file = await Assert.That(result.Document!.Files).HasSingleItem();
            await Assert.That(file.Path).IsEqualTo(Path.Combine("MyApp.Tests", "SampleTests.cs"));

            await Assert.That(file.TestCases.Count).IsEqualTo(4);
            await Assert.That(result.Passed).IsEqualTo(1);
            await Assert.That(result.Skipped).IsEqualTo(1);
            await Assert.That(result.Failed).IsEqualTo(1);
            await Assert.That(result.Errored).IsEqualTo(1);
            await Assert.That(result.Unresolved).IsEqualTo(0);
            await Assert.That(result.TrxFileCount).IsEqualTo(1);

            TestCase passed = file.TestCases.Single(t => t.Name == "PassingTest");
            await Assert.That(passed.Skipped).IsNull();
            await Assert.That(passed.Failure).IsNull();
            await Assert.That(passed.Error).IsNull();
            await Assert.That(passed.Duration).IsEqualTo(15L);

            TestCase failed = file.TestCases.Single(t => t.Name == "FailingTest");
            await Assert.That(failed.Failure).IsNotNull();
            await Assert.That(failed.Error).IsNull();
            await Assert.That(failed.Failure!.Message).IsEqualTo("Assertion failed");
            await Assert.That(failed.Failure!.Value).Contains("at FailingTest");

            TestCase skipped = file.TestCases.Single(t => t.Name == "SkippedTest");
            await Assert.That(skipped.Skipped).IsNotNull();
            await Assert.That(skipped.Failure).IsNull();
            await Assert.That(skipped.Error).IsNull();

            TestCase errored = file.TestCases.Single(t => t.Name == "ErroredTest");
            await Assert.That(errored.Error).IsNotNull();
            await Assert.That(errored.Failure).IsNull();
            await Assert.That(errored.Error!.Message).IsEqualTo("Exception thrown");
        }
        finally
        {
            if (Directory.Exists(solutionDir))
            {
                Directory.Delete(solutionDir, true);
            }
        }
    }

    [Test]
    public async Task Parse_SerializedOutput_ContainsBothErrorAndFailureElements()
    {
        // Round-trip the converted document through Converter.Save to make sure the
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
            await Assert.That(result.Document).IsNotNull();
            await Assert.That(Converter.Save(result.Document!, outputPath)).IsTrue();

            string xml = IOFile.ReadAllText(outputPath);
            await Assert.That(xml).Contains("<failure message=\"Assertion failed\"");
            await Assert.That(xml).Contains("<error message=\"Exception thrown\"");
            await Assert.That(xml).Contains("<skipped");
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
