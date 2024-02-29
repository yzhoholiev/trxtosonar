using System.Xml.Serialization;

namespace TrxToSonar.Model.Sonar;

public sealed class Failure
{
    public Failure()
    {
    }

    public Failure(string? message, string? value)
    {
        Message = message;
        Value = value;
    }

    [XmlAttribute(AttributeName = "message")]
    public string? Message { get; set; }

    [XmlText]
    public string? Value { get; set; }
}
