using System.Xml.Serialization;

namespace TrxToSonar.Model.Sonar;

public sealed class Error
{
    public Error()
    {
    }

    public Error(string? message, string? value)
    {
        Message = message;
        Value = value;
    }

    [XmlAttribute(AttributeName = "message")]
    public string? Message { get; set; }

    [XmlText]
    public string? Value { get; set; }
}
