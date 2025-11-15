using System.Xml;
using System.Xml.Serialization;

namespace TrxToSonar;

internal static class XmlParser<T>
{
    private static readonly XmlSerializer XmlSerializer = new(typeof(T));

    public static bool Save(T xmlDocument, string outputFilename)
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

        try
        {
            File.WriteAllText(outputFilename, xmlContent);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new TrxToSonarException($"Access denied writing to {outputFilename}", ex);
        }
        catch (IOException ex)
        {
            throw new TrxToSonarException($"IO error writing to {outputFilename}", ex);
        }
    }

    public static T? Deserialize(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"XML file not found: {filename}", filename);
        }

        try
        {
            using var streamReader = new StreamReader(filename);
            using var xmlReader = new XmlTextReader(streamReader);
            return (T?) XmlSerializer.Deserialize(xmlReader);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
        {
            throw new TrxToSonarException($"Invalid XML format in file {filename}: {ex.InnerException.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new TrxToSonarException($"Failed to deserialize XML from {filename}", ex);
        }
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
