using System.Xml.Serialization;

namespace TrxToSonar.Model.Sonar;

public class File
{
    public File()
    {
    }

    public File(string? path)
    {
        Path = path;
    }

    [XmlAttribute(AttributeName = "path")]
    public string? Path { get; set; }

    [XmlElement("testCase")]
    public List<TestCase> TestCases { get; set; } = new();
}
