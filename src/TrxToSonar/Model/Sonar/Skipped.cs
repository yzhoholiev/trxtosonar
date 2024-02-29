using System.Xml.Serialization;

namespace TrxToSonar.Model.Sonar;

public sealed class Skipped
{
    [XmlAttribute(AttributeName = "message")]
    public string Message { get; set; } = "Skipped";

    [XmlText]
    public string? Value { get; set; }
}
