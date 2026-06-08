using TrxToSonar;
using TrxToSonar.Sonar.Models;
using TrxToSonar.Trx.Models;

namespace TrxToSonarTest;

public sealed class TestCaseFactoryTests
{
    [Test]
    public async Task Create_PassedOutcome_HasNoOutcomeElements()
    {
        TestCase result = TestCaseFactory.Create(new UnitTestResult(TestName: "T", Duration: "00:00:00.0050000", Outcome: Outcome.Passed));

        await Assert.That(result.Name).IsEqualTo("T");
        await Assert.That(result.Duration).IsEqualTo(5L);
        await Assert.That(result.Skipped).IsNull();
        await Assert.That(result.Failure).IsNull();
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task Create_NotExecutedOutcome_IsSkipped()
    {
        TestCase result = TestCaseFactory.Create(new UnitTestResult(Outcome: Outcome.NotExecuted));

        await Assert.That(result.Skipped).IsNotNull();
        await Assert.That(result.Failure).IsNull();
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task Create_FailedOutcome_CarriesFailureMessageAndStackTrace()
    {
        var trxResult = new UnitTestResult(
            TestName: "T",
            Outcome: Outcome.Failed,
            Output: new Output(null, new ErrorInfo("boom", "at X")));

        TestCase result = TestCaseFactory.Create(trxResult);

        await Assert.That(result.Failure).IsNotNull();
        await Assert.That(result.Failure!.Message).IsEqualTo("boom");
        await Assert.That(result.Failure!.Value).IsEqualTo("at X");
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task Create_ErrorOutcome_CarriesErrorMessageAndStackTrace()
    {
        var trxResult = new UnitTestResult(
            TestName: "T",
            Outcome: Outcome.Error,
            Output: new Output(null, new ErrorInfo("kaboom", "at Y")));

        TestCase result = TestCaseFactory.Create(trxResult);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!.Message).IsEqualTo("kaboom");
        await Assert.That(result.Error!.Value).IsEqualTo("at Y");
        await Assert.That(result.Failure).IsNull();
    }
}
