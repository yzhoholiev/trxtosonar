using TrxToSonar;
using Xunit;

namespace TrxToSonarTest;

public class UtilsTests
{
    [Fact]
    public void TrxDurationShouldBeConverted()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00.0090000");
        Assert.Equal(9, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithNullInput_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration(null);
        Assert.Equal(0, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithEmptyString_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration(string.Empty);
        Assert.Equal(0, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithInvalidFormat_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration("invalid");
        Assert.Equal(0, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithZeroDuration_ReturnsZero()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00");
        Assert.Equal(0, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithSeconds_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:01");
        Assert.Equal(1000, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithMinutes_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:01:00");
        Assert.Equal(60000, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithHours_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("01:00:00");
        Assert.Equal(3600000, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithComplexDuration_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("01:23:45.6789123");
        Assert.Equal(5025678, sonarDuration);
    }

    [Fact]
    public void ToSonarDuration_WithMilliseconds_ReturnsCorrectMilliseconds()
    {
        long sonarDuration = Utils.ToSonarDuration("00:00:00.1234567");
        Assert.Equal(123, sonarDuration);
    }

    [Theory]
    [InlineData("00:00:00.0010000", 1)]
    [InlineData("00:00:00.0100000", 10)]
    [InlineData("00:00:00.1000000", 100)]
    [InlineData("00:00:01.0000000", 1000)]
    [InlineData("00:01:00.0000000", 60000)]
    [InlineData("01:00:00.0000000", 3600000)]
    public void ToSonarDuration_WithVariousDurations_ReturnsCorrectMilliseconds(string trxDuration, long expectedMilliseconds)
    {
        long sonarDuration = Utils.ToSonarDuration(trxDuration);
        Assert.Equal(expectedMilliseconds, sonarDuration);
    }
}
