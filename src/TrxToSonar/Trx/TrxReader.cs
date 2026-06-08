using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using TrxToSonar.Trx.Models;

namespace TrxToSonar.Trx;

/// <summary>
///     Reads a Visual Studio TRX document. Element lookups are namespace-qualified because the TRX
///     root declares a default namespace.
/// </summary>
internal static class TrxReader
{
    /// <summary>
    ///     Reads and parses a TRX file. Returns null for a missing, unreadable, or malformed file so
    ///     one bad TRX is skipped rather than aborting the whole run.
    /// </summary>
    public static TrxDocument? Read(string filename)
    {
        if (!File.Exists(filename))
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(filename);
            return ReadDocument(document);
        }
        catch (XmlException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static TrxDocument ReadDocument(XDocument document)
    {
        var result = new TrxDocument();

        XElement? root = document.Root;
        if (root is null)
        {
            return result;
        }

        XNamespace ns = root.Name.Namespace;

        if (root.Element(ns + "Results") is { } results)
        {
            foreach (XElement element in results.Elements(ns + "UnitTestResult"))
            {
                result.Results.Add(ReadUnitTestResult(element, ns));
            }
        }

        if (root.Element(ns + "TestDefinitions") is { } definitions)
        {
            foreach (XElement element in definitions.Elements(ns + "UnitTest"))
            {
                result.TestDefinitions.Add(ReadUnitTest(element, ns));
            }
        }

        if (root.Element(ns + "ResultSummary") is { } summary)
        {
            result.ResultSummary = ReadResultSummary(summary, ns);
        }

        return result;
    }

    private static UnitTestResult ReadUnitTestResult(XElement element, XNamespace ns)
    {
        return new UnitTestResult(
            (string?) element.Attribute("executionId"),
            (string?) element.Attribute("testId"),
            (string?) element.Attribute("testName"),
            (string?) element.Attribute("duration"),
            ReadDateTime(element.Attribute("startTime")),
            ReadDateTime(element.Attribute("endTime")),
            ReadOutcome((string?) element.Attribute("outcome")),
            element.Element(ns + "Output") is { } output ? ReadOutput(output, ns) : null);
    }

    private static UnitTest ReadUnitTest(XElement element, XNamespace ns)
    {
        return new UnitTest(
            (string?) element.Attribute("name"),
            (string?) element.Attribute("storage"),
            (string?) element.Attribute("id"),
            element.Element(ns + "Execution") is { } execution
                ? new Execution((string?) execution.Attribute("id"))
                : null,
            element.Element(ns + "TestMethod") is { } testMethod
                ? new TestMethod(
                    (string?) testMethod.Attribute("codeBase") ?? string.Empty,
                    (string?) testMethod.Attribute("className"),
                    (string?) testMethod.Attribute("name"))
                : null);
    }

    private static Output ReadOutput(XElement element, XNamespace ns)
    {
        return new Output(
            (string?) element.Element(ns + "StdOut"),
            element.Element(ns + "ErrorInfo") is { } errorInfo
                ? new ErrorInfo(
                    (string?) errorInfo.Element(ns + "Message"),
                    (string?) errorInfo.Element(ns + "StackTrace"))
                : null);
    }

    private static ResultSummary ReadResultSummary(XElement element, XNamespace ns)
    {
        return new ResultSummary(
            (string?) element.Attribute("outcome"),
            element.Element(ns + "Counters") is { } counters ? ReadCounters(counters) : null,
            element.Element(ns + "Output") is { } output ? ReadOutput(output, ns) : null);
    }

    private static Counters ReadCounters(XElement element)
    {
        return new Counters(
            ReadInt(element, "total"),
            ReadInt(element, "executed"),
            ReadInt(element, "passed"),
            ReadInt(element, "failed"),
            ReadInt(element, "error"),
            ReadInt(element, "timeout"),
            ReadInt(element, "aborted"),
            ReadInt(element, "inconclusive"),
            ReadInt(element, "passedButRunAborted"),
            ReadInt(element, "notRunnable"),
            ReadInt(element, "notExecuted"),
            ReadInt(element, "disconnected"),
            ReadInt(element, "warning"),
            ReadInt(element, "completed"),
            ReadInt(element, "inProgress"),
            ReadInt(element, "pending"));
    }

    private static int ReadInt(XElement element, string name)
    {
        return int.TryParse((string?) element.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;
    }

    private static DateTime ReadDateTime(XAttribute? attribute)
    {
        return attribute is not null && DateTime.TryParse(attribute.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime value)
            ? value
            : default;
    }

    private static Outcome ReadOutcome(string? value)
    {
        return Enum.TryParse(value, out Outcome outcome) ? outcome : Outcome.Error;
    }
}
