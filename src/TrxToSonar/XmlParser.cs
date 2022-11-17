using System.Xml;
using System.Xml.Serialization;

namespace TrxToSonar;

public sealed class XmlParser<T>
{
    private static readonly XmlSerializer XmlSerializer = new(typeof(T));

    public bool Save(T xmlDocument, string outputFilename)
    {
        string xmlContent = Serialize(xmlDocument);

        if (string.IsNullOrEmpty(xmlContent))
        {
            return false;
        }

        var fileInfo = new FileInfo(outputFilename);

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }
        else if (fileInfo.Directory?.Exists == false)
        {
            fileInfo.Directory.Create();
        }

        File.WriteAllText(outputFilename, xmlContent);
        return true;
    }

    public T? Deserialize(string filename)
    {
        using var streamReader = new StreamReader(filename);
        using var xmlReader = new XmlTextReader(streamReader);
        return (T?) XmlSerializer.Deserialize(xmlReader);
    }

    private static string Serialize(T? xmlDocument)
    {
        var emptyNamespaces = new XmlSerializerNamespaces();
        emptyNamespaces.Add("", "");

        var xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true
        };

        using var streamWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(streamWriter, xmlSettings);
        XmlSerializer.Serialize(xmlWriter, xmlDocument, emptyNamespaces);
        return streamWriter.ToString();
    }
}
