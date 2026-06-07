using TrxToSonar.Trx;
using TrxToSonar.Trx.Models;
using IOFile = System.IO.File;

namespace TrxToSonarTest.Trx;

public class TrxReaderTests
{
    private const string SampleTrx = """
                                     <?xml version="1.0" encoding="UTF-8"?>
                                     <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                                       <Results>
                                         <UnitTestResult executionId="e1" testId="t1" testName="PassingTest" duration="00:00:00.0150000" outcome="Passed" />
                                         <UnitTestResult executionId="e2" testId="t2" testName="FailingTest" duration="00:00:00.0200000" outcome="Failed">
                                           <Output>
                                             <ErrorInfo>
                                               <Message>boom</Message>
                                               <StackTrace>at FailingTest</StackTrace>
                                             </ErrorInfo>
                                           </Output>
                                         </UnitTestResult>
                                       </Results>
                                       <TestDefinitions>
                                         <UnitTest id="t1" name="PassingTest" storage="mytests.dll">
                                           <Execution id="e1" />
                                           <TestMethod codeBase="C:\bin\MyTests.dll" className="MyNamespace.MyTests" name="PassingTest" />
                                         </UnitTest>
                                         <UnitTest id="t2" name="FailingTest" storage="mytests.dll">
                                           <Execution id="e2" />
                                           <TestMethod codeBase="C:\bin\MyTests.dll" className="MyNamespace.MyTests" name="FailingTest" />
                                         </UnitTest>
                                       </TestDefinitions>
                                       <ResultSummary outcome="Failed">
                                         <Counters total="2" executed="2" passed="1" failed="1" />
                                       </ResultSummary>
                                     </TestRun>
                                     """;

    [Test]
    public async Task Read_WithMissingFile_ReturnsNull()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.trx");

        await Assert.That(TrxReader.Read(path)).IsNull();
    }

    [Test]
    public async Task Read_WithMalformedXml_ReturnsNull()
    {
        string path = WriteTrx("<TestRun><Results>");

        try
        {
            await Assert.That(TrxReader.Read(path)).IsNull();
        }
        finally
        {
            IOFile.Delete(path);
        }
    }

    [Test]
    public async Task Read_WithValidTrx_ParsesResultsAndDefinitions()
    {
        string path = WriteTrx(SampleTrx);

        try
        {
            TrxDocument? document = TrxReader.Read(path);

            await Assert.That(document).IsNotNull();
            await Assert.That(document!.Results.Count).IsEqualTo(2);
            await Assert.That(document.TestDefinitions.Count).IsEqualTo(2);

            UnitTestResult passing = document.Results.Single(r => r.TestName == "PassingTest");
            await Assert.That(passing.Outcome).IsEqualTo(Outcome.Passed);
            await Assert.That(passing.Output).IsNull();

            UnitTestResult failing = document.Results.Single(r => r.TestName == "FailingTest");
            await Assert.That(failing.TestId).IsEqualTo("t2");
            await Assert.That(failing.Outcome).IsEqualTo(Outcome.Failed);
            await Assert.That(failing.Output).IsNotNull();
            await Assert.That(failing.Output!.ErrorInfo).IsNotNull();
            await Assert.That(failing.Output!.ErrorInfo!.Message).IsEqualTo("boom");
            await Assert.That(failing.Output!.ErrorInfo!.StackTrace).IsEqualTo("at FailingTest");

            UnitTest definition = document.TestDefinitions.Single(d => d.Id == "t2");
            await Assert.That(definition.Storage).IsEqualTo("mytests.dll");
            await Assert.That(definition.Execution).IsNotNull();
            await Assert.That(definition.Execution!.Id).IsEqualTo("e2");
            await Assert.That(definition.TestMethod).IsNotNull();
            await Assert.That(definition.TestMethod!.ClassName).IsEqualTo("MyNamespace.MyTests");
            await Assert.That(definition.TestMethod!.CodeBase).IsEqualTo(@"C:\bin\MyTests.dll");
        }
        finally
        {
            IOFile.Delete(path);
        }
    }

    [Test]
    public async Task Read_WithValidTrx_ParsesResultSummary()
    {
        string path = WriteTrx(SampleTrx);

        try
        {
            TrxDocument? document = TrxReader.Read(path);

            await Assert.That(document).IsNotNull();
            await Assert.That(document!.ResultSummary).IsNotNull();
            await Assert.That(document.ResultSummary!.Outcome).IsEqualTo("Failed");
            await Assert.That(document.ResultSummary!.Counters).IsNotNull();
            await Assert.That(document.ResultSummary!.Counters!.Total).IsEqualTo(2);
            await Assert.That(document.ResultSummary!.Counters!.Passed).IsEqualTo(1);
            await Assert.That(document.ResultSummary!.Counters!.Failed).IsEqualTo(1);
        }
        finally
        {
            IOFile.Delete(path);
        }
    }

    [Test]
    public async Task Read_WithUnrecognizedOutcome_DefaultsToError()
    {
        string path = WriteTrx(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="t1" testName="X" duration="00:00:00" outcome="Bogus" />
              </Results>
            </TestRun>
            """);

        try
        {
            TrxDocument? document = TrxReader.Read(path);

            await Assert.That(document).IsNotNull();
            UnitTestResult result = document!.Results.Single();
            await Assert.That(result.Outcome).IsEqualTo(Outcome.Error);
        }
        finally
        {
            IOFile.Delete(path);
        }
    }

    private static string WriteTrx(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.trx");
        IOFile.WriteAllText(path, content);
        return path;
    }
}
