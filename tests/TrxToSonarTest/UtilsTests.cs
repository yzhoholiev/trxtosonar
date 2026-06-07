using TrxToSonar;

namespace TrxToSonarTest;

public class UtilsTests
{
    [Test]
    public async Task TrxDurationShouldBeConverted()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00.0090000");
        await Assert.That(sonarDuration).IsEqualTo(9L);
    }

    [Test]
    public async Task ToSonarDuration_WithNullInput_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration(null);
        await Assert.That(sonarDuration).IsEqualTo(0L);
    }

    [Test]
    public async Task ToSonarDuration_WithEmptyString_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration(string.Empty);
        await Assert.That(sonarDuration).IsEqualTo(0L);
    }

    [Test]
    public async Task ToSonarDuration_WithInvalidFormat_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration("invalid");
        await Assert.That(sonarDuration).IsEqualTo(0L);
    }

    [Test]
    public async Task ToSonarDuration_WithZeroDuration_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00");
        await Assert.That(sonarDuration).IsEqualTo(0L);
    }

    [Test]
    public async Task ToSonarDuration_WithSeconds_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:01");
        await Assert.That(sonarDuration).IsEqualTo(1000L);
    }

    [Test]
    public async Task ToSonarDuration_WithMinutes_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:01:00");
        await Assert.That(sonarDuration).IsEqualTo(60000L);
    }

    [Test]
    public async Task ToSonarDuration_WithHours_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("01:00:00");
        await Assert.That(sonarDuration).IsEqualTo(3600000L);
    }

    [Test]
    public async Task ToSonarDuration_WithComplexDuration_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("01:23:45.6789123");
        await Assert.That(sonarDuration).IsEqualTo(5025678L);
    }

    [Test]
    public async Task ToSonarDuration_WithMilliseconds_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00.1234567");
        await Assert.That(sonarDuration).IsEqualTo(123L);
    }

    [Test]
    [Arguments("00:00:00.0010000", 1L)]
    [Arguments("00:00:00.0100000", 10L)]
    [Arguments("00:00:00.1000000", 100L)]
    [Arguments("00:00:01.0000000", 1000L)]
    [Arguments("00:01:00.0000000", 60000L)]
    [Arguments("01:00:00.0000000", 3600000L)]
    public async Task ToSonarDuration_WithVariousDurations_ReturnsCorrectMilliseconds(string trxDuration, long expectedMilliseconds)
    {
        long sonarDuration = Utils.ToSonarDuration(trxDuration);
        await Assert.That(sonarDuration).IsEqualTo(expectedMilliseconds);
    }
}
