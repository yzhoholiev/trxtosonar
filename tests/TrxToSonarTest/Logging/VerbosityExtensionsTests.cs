using Microsoft.Extensions.Logging;
using TrxToSonar.Logging;

namespace TrxToSonarTest.Logging;

public sealed class VerbosityExtensionsTests
{
    [Test]
    public async Task ToLogLevel_Quiet_MapsToError()
    {
        await Assert.That(Verbosity.Quiet.ToLogLevel()).IsEqualTo(LogLevel.Error);
    }

    [Test]
    public async Task ToLogLevel_Minimal_MapsToWarning()
    {
        await Assert.That(Verbosity.Minimal.ToLogLevel()).IsEqualTo(LogLevel.Warning);
    }

    [Test]
    public async Task ToLogLevel_Normal_MapsToInformation()
    {
        await Assert.That(Verbosity.Normal.ToLogLevel()).IsEqualTo(LogLevel.Information);
    }

    [Test]
    public async Task ToLogLevel_Detailed_MapsToDebug()
    {
        await Assert.That(Verbosity.Detailed.ToLogLevel()).IsEqualTo(LogLevel.Debug);
    }

    [Test]
    public async Task ToLogLevel_Diagnostic_MapsToTrace()
    {
        await Assert.That(Verbosity.Diagnostic.ToLogLevel()).IsEqualTo(LogLevel.Trace);
    }
}
