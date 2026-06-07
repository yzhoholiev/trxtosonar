using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using TrxToSonar.Sonar.Models;
using File = TrxToSonar.Sonar.Models.File;

namespace TrxToSonar.Sonar;

/// <summary>
///     Writes the SonarQube generic test-data XML using LINQ to XML over an XmlWriter sink.
///     Reflection-free and Native AOT safe.
/// </summary>
internal static class SonarWriter
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = true
    };

    public static bool Write(SonarDocument document, string outputFilename)
    {
        string xml = Serialize(document);

        if (string.IsNullOrEmpty(xml))
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
            System.IO.File.WriteAllText(outputFilename, xml);
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

    private static string Serialize(SonarDocument document)
    {
        XElement root = BuildDocument(document);

        using var streamWriter = new StringWriter();

        using (var writer = XmlWriter.Create(streamWriter, WriterSettings))
        {
            root.Save(writer);
        }

        return streamWriter.ToString();
    }

    private static XElement BuildDocument(SonarDocument document)
    {
        var root = new XElement("testExecutions", new XAttribute("version", document.Version.ToString(CultureInfo.InvariantCulture)));

        foreach (File file in document.Files)
        {
            root.Add(BuildFile(file));
        }

        return root;
    }

    private static XElement BuildFile(File file)
    {
        var element = new XElement("file");
        AddOptionalAttribute(element, "path", file.Path);

        foreach (TestCase testCase in file.TestCases)
        {
            element.Add(BuildTestCase(testCase));
        }

        return element;
    }

    private static XElement BuildTestCase(TestCase testCase)
    {
        var element = new XElement("testCase");
        AddOptionalAttribute(element, "name", testCase.Name);
        element.Add(new XAttribute("duration", testCase.Duration.ToString(CultureInfo.InvariantCulture)));

        if (testCase.Error is { } error)
        {
            element.Add(BuildMessageElement("error", error.Message, error.Value));
        }

        if (testCase.Skipped is { } skipped)
        {
            element.Add(BuildMessageElement("skipped", skipped.Message, skipped.Value));
        }

        if (testCase.Failure is { } failure)
        {
            element.Add(BuildMessageElement("failure", failure.Message, failure.Value));
        }

        return element;
    }

    private static XElement BuildMessageElement(string name, string? message, string? value)
    {
        var element = new XElement(name);
        AddOptionalAttribute(element, "message", message);

        if (value is not null)
        {
            element.Add(value);
        }

        return element;
    }

    private static void AddOptionalAttribute(XElement element, string name, string? value)
    {
        if (value is not null)
        {
            element.Add(new XAttribute(name, value));
        }
    }
}
