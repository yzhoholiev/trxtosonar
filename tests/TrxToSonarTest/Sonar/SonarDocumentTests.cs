using TrxToSonar.Sonar.Models;
using File = TrxToSonar.Sonar.Models.File;

namespace TrxToSonarTest.Sonar;

public sealed class SonarDocumentTests
{
    [Test]
    public async Task AddTestCase_NewPath_CreatesFileWithTestCase()
    {
        var document = new SonarDocument();

        document.AddTestCase("Tests/MyTests.cs", new TestCase("PassedTest", 5));

        File file = await Assert.That(document.Files).HasSingleItem();
        await Assert.That(file.Path).IsEqualTo("Tests/MyTests.cs");
        await Assert.That(file.TestCases.Count).IsEqualTo(1);
        await Assert.That(file.TestCases[0].Name).IsEqualTo("PassedTest");
    }

    [Test]
    public async Task AddTestCase_SamePathTwice_GroupsTestCasesIntoOneFile()
    {
        var document = new SonarDocument();

        document.AddTestCase("Tests/MyTests.cs", new TestCase("First", 1));
        document.AddTestCase("Tests/MyTests.cs", new TestCase("Second", 2));

        File file = await Assert.That(document.Files).HasSingleItem();
        await Assert.That(file.TestCases.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AddTestCase_DifferentPaths_CreatesSeparateFiles()
    {
        var document = new SonarDocument();

        document.AddTestCase("Tests/A.cs", new TestCase("A1", 1));
        document.AddTestCase("Tests/B.cs", new TestCase("B1", 1));

        await Assert.That(document.Files.Count).IsEqualTo(2);
    }
}
