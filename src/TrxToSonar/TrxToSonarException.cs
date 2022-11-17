using System.Runtime.Serialization;

namespace TrxToSonar;

[Serializable]
public class TrxToSonarException : Exception
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

    protected TrxToSonarException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
