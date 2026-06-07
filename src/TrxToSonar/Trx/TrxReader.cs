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
        return new UnitTestResult
        {
            ExecutionId = (string?) element.Attribute("executionId"),
            TestId = (string?) element.Attribute("testId"),
            TestName = (string?) element.Attribute("testName"),
            Duration = (string?) element.Attribute("duration"),
            StartTime = ReadDateTime(element.Attribute("startTime")),
            EndTime = ReadDateTime(element.Attribute("endTime")),
            Outcome = ReadOutcome((string?) element.Attribute("outcome")),
            Output = element.Element(ns + "Output") is { } output ? ReadOutput(output, ns) : null
        };
    }

    private static UnitTest ReadUnitTest(XElement element, XNamespace ns)
    {
        return new UnitTest
        {
            Name = (string?) element.Attribute("name"),
            Storage = (string?) element.Attribute("storage"),
            Id = (string?) element.Attribute("id"),
            Execution = element.Element(ns + "Execution") is { } execution
                ? new Execution { Id = (string?) execution.Attribute("id") }
                : null,
            TestMethod = element.Element(ns + "TestMethod") is { } testMethod
                ? new TestMethod
                {
                    CodeBase = (string?) testMethod.Attribute("codeBase") ?? string.Empty,
                    ClassName = (string?) testMethod.Attribute("className"),
                    Name = (string?) testMethod.Attribute("name")
                }
                : null
        };
    }

    private static Output ReadOutput(XElement element, XNamespace ns)
    {
        return new Output
        {
            StdOut = (string?) element.Element(ns + "StdOut"),
            ErrorInfo = element.Element(ns + "ErrorInfo") is { } errorInfo
                ? new ErrorInfo
                {
                    Message = (string?) errorInfo.Element(ns + "Message"),
                    StackTrace = (string?) errorInfo.Element(ns + "StackTrace")
                }
                : null
        };
    }

    private static ResultSummary ReadResultSummary(XElement element, XNamespace ns)
    {
        return new ResultSummary
        {
            Outcome = (string?) element.Attribute("outcome"),
            Counters = element.Element(ns + "Counters") is { } counters ? ReadCounters(counters) : null,
            Output = element.Element(ns + "Output") is { } output ? ReadOutput(output, ns) : null
        };
    }

    private static Counters ReadCounters(XElement element)
    {
        return new Counters
        {
            Total = ReadInt(element, "total"),
            Executed = ReadInt(element, "executed"),
            Passed = ReadInt(element, "passed"),
            Failed = ReadInt(element, "failed"),
            Error = ReadInt(element, "error"),
            Timeout = ReadInt(element, "timeout"),
            Aborted = ReadInt(element, "aborted"),
            Inconclusive = ReadInt(element, "inconclusive"),
            PassedButRunAborted = ReadInt(element, "passedButRunAborted"),
            NotRunnable = ReadInt(element, "notRunnable"),
            NotExecuted = ReadInt(element, "notExecuted"),
            Disconnected = ReadInt(element, "disconnected"),
            Warning = ReadInt(element, "warning"),
            Completed = ReadInt(element, "completed"),
            InProgress = ReadInt(element, "inProgress"),
            Pending = ReadInt(element, "pending")
        };
    }

    private static int ReadInt(XElement element, string name)
    {
        return int.TryParse((string?) element.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;
    }

    private static DateTime ReadDateTime(XAttribute? attribute)
    {
        if (attribute is null)
        {
            return default;
        }

        return DateTime.TryParse(attribute.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime value)
            ? value
            : default;
    }

    private static Outcome ReadOutcome(string? value)
    {
        return Enum.TryParse(value, out Outcome outcome) ? outcome : Outcome.Error;
    }
}
