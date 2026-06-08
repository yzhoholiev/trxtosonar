using TrxToSonar;
using TrxToSonar.Trx.Models;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class TestFileResolverTests
{
    [Test]
    public async Task Resolve_WithNullClassName_ReturnsFailure()
    {
        var unitTest = new UnitTest(TestMethod: new TestMethod(@"C:\Projects\Tests\bin\Debug", null));

        var resolver = new TestFileResolver(@"C:\Projects", false);

        Result<string> result = resolver.Resolve(unitTest);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Class name was not provided");
    }

    [Test]
    public async Task Resolve_WithEmptyClassName_ReturnsFailure()
    {
        var unitTest = new UnitTest(TestMethod: new TestMethod(@"C:\Projects\Tests\bin\Debug", string.Empty));

        var resolver = new TestFileResolver(@"C:\Projects", false);

        Result<string> result = resolver.Resolve(unitTest);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Class name was not provided");
    }

    [Test]
    public async Task Resolve_WithNullUnitTest_ReturnsFailure()
    {
        var resolver = new TestFileResolver(@"C:\Projects", false);

        Result<string> result = resolver.Resolve(null);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Class name was not provided");
    }

    [Test]
    public async Task Resolve_WithFileNotFound_ReturnsFailure()
    {
        using var temp = new TempProject();
        var unitTest = new UnitTest(TestMethod: new TestMethod(temp.BinDir, "MyNamespace.NonExistentTestClass"));

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> result = resolver.Resolve(unitTest);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Cannot find file with class NonExistentTestClass");
    }

    [Test]
    public async Task Resolve_WithAbsolutePath_ReturnsFullPath()
    {
        using var temp = new TempProject();
        string testFile = temp.WriteSource("MyTestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, true);

        Result<string> result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyTestClass"));

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(testFile);
        await Assert.That(Path.IsPathRooted(result.Value)).IsTrue();
    }

    [Test]
    public async Task Resolve_WithRelativePath_ReturnsRelativePath()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyTestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyTestClass"));

        await Assert.That(result.Value).IsEqualTo(Path.Combine("Tests", "MyTestClass.cs"));
        await Assert.That(Path.IsPathRooted(result.Value)).IsFalse();
    }

    [Test]
    public async Task Resolve_FindsFileWithTestSuffix()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyClassTest.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyClass"));

        await Assert.That(result.Value).IsEqualTo(Path.Combine("Tests", "MyClassTest.cs"));
    }

    [Test]
    public async Task Resolve_FindsFileWithTestsSuffix()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyClassTests.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyClass"));

        await Assert.That(result.Value).IsEqualTo(Path.Combine("Tests", "MyClassTests.cs"));
    }

    [Test]
    public async Task Resolve_ExtractsClassNameFromFullyQualifiedName()
    {
        using var temp = new TempProject();
        temp.WriteSource("TestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> result = resolver.Resolve(temp.MakeUnitTest("My.Very.Long.Namespace.TestClass"));

        await Assert.That(result.Value).IsEqualTo(Path.Combine("Tests", "TestClass.cs"));
    }

    [Test]
    public async Task Resolve_CachesProjectFiles_AcrossMultipleCalls()
    {
        // Walk the same project tree twice and add a new source file in between.
        // If the cache is doing its job, the second call won't see the new file.
        using var temp = new TempProject();
        temp.WriteSource("FirstClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, false);

        Result<string> first = resolver.Resolve(temp.MakeUnitTest("MyNamespace.FirstClass"));
        await Assert.That(first.Value).IsEqualTo(Path.Combine("Tests", "FirstClass.cs"));

        // Add a second source file AFTER the cache is populated.
        temp.WriteSource("SecondClass.cs");

        Result<string> second = resolver.Resolve(temp.MakeUnitTest("MyNamespace.SecondClass"));
        await Assert.That(second.IsSuccess).IsFalse();
    }

    private sealed class TempProject : IDisposable
    {
        public TempProject()
        {
            Directory.CreateDirectory(BinDir);
        }

        public string SolutionDir { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public string ProjectRoot => Path.Combine(SolutionDir, "Tests");

        public string BinDir => Path.Combine(ProjectRoot, "bin");

        public void Dispose()
        {
            if (Directory.Exists(SolutionDir))
            {
                Directory.Delete(SolutionDir, true);
            }
        }

        public string WriteSource(string filename)
        {
            string path = Path.Combine(ProjectRoot, filename);
            IOFile.WriteAllText(path, "// test file");
            return path;
        }

        public UnitTest MakeUnitTest(string className)
        {
            return new UnitTest(TestMethod: new TestMethod(BinDir, className));
        }
    }
}
