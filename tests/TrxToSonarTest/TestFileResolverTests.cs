using TrxToSonar;
using TrxToSonar.Model.Trx;
using Xunit;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class TestFileResolverTests
{
    [Fact]
    public void Resolve_WithNullClassName_ThrowsException()
    {
        var unitTest = new UnitTest
        {
            TestMethod = new TestMethod
            {
                ClassName = null,
                CodeBase = @"C:\Projects\Tests\bin\Debug"
            }
        };

        var resolver = new TestFileResolver(@"C:\Projects", useAbsolutePath: false);

        TrxToSonarException exception = Assert.Throws<TrxToSonarException>(() => resolver.Resolve(unitTest));
        Assert.Equal("Class name was not provided", exception.Message);
    }

    [Fact]
    public void Resolve_WithEmptyClassName_ThrowsException()
    {
        var unitTest = new UnitTest
        {
            TestMethod = new TestMethod
            {
                ClassName = string.Empty,
                CodeBase = @"C:\Projects\Tests\bin\Debug"
            }
        };

        var resolver = new TestFileResolver(@"C:\Projects", useAbsolutePath: false);

        TrxToSonarException exception = Assert.Throws<TrxToSonarException>(() => resolver.Resolve(unitTest));
        Assert.Equal("Class name was not provided", exception.Message);
    }

    [Fact]
    public void Resolve_WithNullUnitTest_ThrowsException()
    {
        var resolver = new TestFileResolver(@"C:\Projects", useAbsolutePath: false);

        Assert.Throws<TrxToSonarException>(() => resolver.Resolve(null));
    }

    [Fact]
    public void Resolve_WithFileNotFound_ThrowsFileNotFoundException()
    {
        using var temp = new TempProject();
        var unitTest = new UnitTest
        {
            TestMethod = new TestMethod
            {
                ClassName = "MyNamespace.NonExistentTestClass",
                CodeBase = temp.BinDir
            }
        };

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() => resolver.Resolve(unitTest));
        Assert.Contains("Cannot find file with class NonExistentTestClass", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WithAbsolutePath_ReturnsFullPath()
    {
        using var temp = new TempProject();
        string testFile = temp.WriteSource("MyTestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: true);

        string result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyTestClass"));

        Assert.Equal(testFile, result);
        Assert.True(Path.IsPathRooted(result));
    }

    [Fact]
    public void Resolve_WithRelativePath_ReturnsRelativePath()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyTestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        string result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyTestClass"));

        Assert.Equal(Path.Combine("Tests", "MyTestClass.cs"), result);
        Assert.False(Path.IsPathRooted(result));
    }

    [Fact]
    public void Resolve_FindsFileWithTestSuffix()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyClassTest.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        string result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyClass"));

        Assert.Equal(Path.Combine("Tests", "MyClassTest.cs"), result);
    }

    [Fact]
    public void Resolve_FindsFileWithTestsSuffix()
    {
        using var temp = new TempProject();
        temp.WriteSource("MyClassTests.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        string result = resolver.Resolve(temp.MakeUnitTest("MyNamespace.MyClass"));

        Assert.Equal(Path.Combine("Tests", "MyClassTests.cs"), result);
    }

    [Fact]
    public void Resolve_ExtractsClassNameFromFullyQualifiedName()
    {
        using var temp = new TempProject();
        temp.WriteSource("TestClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        string result = resolver.Resolve(temp.MakeUnitTest("My.Very.Long.Namespace.TestClass"));

        Assert.Equal(Path.Combine("Tests", "TestClass.cs"), result);
    }

    [Fact]
    public void Resolve_CachesProjectFiles_AcrossMultipleCalls()
    {
        // Walk the same project tree twice and add a new source file in between.
        // If the cache is doing its job, the second call won't see the new file.
        using var temp = new TempProject();
        temp.WriteSource("FirstClass.cs");

        var resolver = new TestFileResolver(temp.SolutionDir, useAbsolutePath: false);

        string first = resolver.Resolve(temp.MakeUnitTest("MyNamespace.FirstClass"));
        Assert.Equal(Path.Combine("Tests", "FirstClass.cs"), first);

        // Add a second source file AFTER the cache is populated.
        temp.WriteSource("SecondClass.cs");

        Assert.Throws<FileNotFoundException>(() => resolver.Resolve(temp.MakeUnitTest("MyNamespace.SecondClass")));
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

        public UnitTest MakeUnitTest(string className) => new()
        {
            TestMethod = new TestMethod
            {
                ClassName = className,
                CodeBase = BinDir
            }
        };
    }
}
