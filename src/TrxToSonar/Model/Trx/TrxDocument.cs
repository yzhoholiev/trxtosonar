using System.Xml.Serialization;

namespace TrxToSonar.Model.Trx;

[XmlRoot(ElementName = "TestRun", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")]
public class TrxDocument
{
    [XmlArray("Results")]
    [XmlArrayItem("UnitTestResult")]
    public List<UnitTestResult> Results { get; } = [];

    [XmlArray("TestDefinitions")]
    [XmlArrayItem("UnitTest")]
    public List<UnitTest> TestDefinitions { get; } = [];

    [XmlElement(ElementName = "ResultSummary")]
    public ResultSummary? ResultSummary { get; set; }
}
