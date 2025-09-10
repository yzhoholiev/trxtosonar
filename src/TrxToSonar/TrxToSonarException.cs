namespace TrxToSonar;

public sealed class TrxToSonarException : Exception
{
    public TrxToSonarException(string message)
        : base(message)
    {
    }

    public TrxToSonarException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TrxToSonarException()
    {
    }
}
