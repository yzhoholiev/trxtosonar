using TrxToSonar;
using TrxToSonar.Model.Sonar;
using TrxToSonar.Model.Trx;
using Xunit;
using File = TrxToSonar.Model.Sonar.File;
using IOFile = System.IO.File;

namespace TrxToSonarTest;

public class ExtensionsTests
{
    [Fact]
    public void GetUnitTest_WithMatchingId_ReturnsUnitTest()
    {
        // Arrange
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest { Id = "test-123", Name = "TestMethod1" };
        trxDocument.TestDefinitions.Add(unitTest);

        var unitTestResult = new UnitTestResult { TestId = "test-123" };

        // Act
        UnitTest? result = unitTestResult.GetUnitTest(trxDocument);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-123", result.Id);
        Assert.Equal("TestMethod1", result.Name);
    }

    [Fact]
    public void GetUnitTest_WithNonMatchingId_ReturnsNull()
    {
        // Arrange
        var trxDocument = new TrxDocument();
        var unitTest = new UnitTest { Id = "test-123", Name = "TestMethod1" };
        trxDocument.TestDefinitions.Add(unitTest);

        var unitTestResult = new UnitTestResult { TestId = "test-456" };

        // Act
        UnitTest? result = unitTestResult.GetUnitTest(trxDocument);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetFile_WithMatchingPath_ReturnsFile()
    {
        // Arrange
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        // Act
        File? result = sonarDocument.GetFile("path/to/test.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("path/to/test.cs", result.Path);
    }

    [Fact]
    public void GetFile_WithNonMatchingPath_ReturnsNull()
    {
        // Arrange
        var sonarDocument = new SonarDocument();
        var file = new File("path/to/test.cs");
        sonarDocument.Files.Add(file);

        // Act
        File? result = sonarDocument.GetFile("path/to/other.cs");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTestFile_WithNullClassName_ThrowsException()
    {
        // Arrange
        var unitTest = new UnitTest
        {
            TestMethod = new TestMethod
            {
                ClassName = null,
                CodeBase = @"C:\Projects\Tests\bin\Debug"
            }
        };

        // Act & Assert
        TrxToSonarException exception = Assert.Throws<TrxToSonarException>(() =>
            unitTest.GetTestFile(@"C:\Projects", false));
        Assert.Equal("Class name was not provided", exception.Message);
    }

    [Fact]
    public void GetTestFile_WithEmptyClassName_ThrowsException()
    {
        // Arrange
        var unitTest = new UnitTest
        {
            TestMethod = new TestMethod
            {
                ClassName = string.Empty,
                CodeBase = @"C:\Projects\Tests\bin\Debug"
            }
        };

        // Act & Assert
        TrxToSonarException exception = Assert.Throws<TrxToSonarException>(() =>
            unitTest.GetTestFile(@"C:\Projects", false));
        Assert.Equal("Class name was not provided", exception.Message);
    }

    [Fact]
    public void GetTestFile_WithNullUnitTest_ThrowsException()
    {
        // Arrange
        UnitTest? unitTest = null;

        // Act & Assert
        Assert.Throws<TrxToSonarException>(() =>
            unitTest.GetTestFile(@"C:\Projects", false));
    }

    [Fact]
    public void GetTestFile_WithFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testDir = Path.Combine(tempDir, "Tests", "bin");
        Directory.CreateDirectory(testDir);

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "MyNamespace.NonExistentTestClass",
                    CodeBase = testDir
                }
            };

            // Act & Assert
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() =>
                unitTest.GetTestFile(tempDir, false));
            Assert.Contains("Cannot find file with class NonExistentTestClass", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetTestFile_WithAbsolutePath_ReturnsFullPath()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testRoot = Path.Combine(tempDir, "Tests");
        string testDir = Path.Combine(testRoot, "bin");
        Directory.CreateDirectory(testDir);

        string testFile = Path.Combine(testRoot, "MyTestClass.cs");
        IOFile.WriteAllText(testFile, "// test file");

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "MyNamespace.MyTestClass",
                    CodeBase = testDir
                }
            };

            // Act
            string result = unitTest.GetTestFile(tempDir, true);

            // Assert
            Assert.Equal(testFile, result);
            Assert.True(Path.IsPathRooted(result));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetTestFile_WithRelativePath_ReturnsRelativePath()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testRoot = Path.Combine(tempDir, "Tests");
        string testDir = Path.Combine(testRoot, "bin");
        Directory.CreateDirectory(testDir);

        string testFile = Path.Combine(testRoot, "MyTestClass.cs");
        IOFile.WriteAllText(testFile, "// test file");

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "MyNamespace.MyTestClass",
                    CodeBase = testDir
                }
            };

            // Act
            string result = unitTest.GetTestFile(tempDir, false);

            // Assert
            Assert.Equal(Path.Combine("Tests", "MyTestClass.cs"), result);
            Assert.False(Path.IsPathRooted(result));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetTestFile_FindsFileWithTestSuffix()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testRoot = Path.Combine(tempDir, "Tests");
        string testDir = Path.Combine(testRoot, "bin");
        Directory.CreateDirectory(testDir);

        string testFile = Path.Combine(testRoot, "MyClassTest.cs");
        IOFile.WriteAllText(testFile, "// test file");

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "MyNamespace.MyClass",
                    CodeBase = testDir
                }
            };

            // Act
            string result = unitTest.GetTestFile(tempDir, false);

            // Assert
            Assert.Equal(Path.Combine("Tests", "MyClassTest.cs"), result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetTestFile_FindsFileWithTestsSuffix()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testRoot = Path.Combine(tempDir, "Tests");
        string testDir = Path.Combine(testRoot, "bin");
        Directory.CreateDirectory(testDir);

        string testFile = Path.Combine(testRoot, "MyClassTests.cs");
        IOFile.WriteAllText(testFile, "// test file");

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "MyNamespace.MyClass",
                    CodeBase = testDir
                }
            };

            // Act
            string result = unitTest.GetTestFile(tempDir, false);

            // Assert
            Assert.Equal(Path.Combine("Tests", "MyClassTests.cs"), result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetTestFile_ExtractsClassNameFromFullyQualifiedName()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string testRoot = Path.Combine(tempDir, "Tests");
        string testDir = Path.Combine(testRoot, "bin");
        Directory.CreateDirectory(testDir);

        string testFile = Path.Combine(testRoot, "TestClass.cs");
        IOFile.WriteAllText(testFile, "// test file");

        try
        {
            var unitTest = new UnitTest
            {
                TestMethod = new TestMethod
                {
                    ClassName = "My.Very.Long.Namespace.TestClass",
                    CodeBase = testDir
                }
            };

            // Act
            string result = unitTest.GetTestFile(tempDir, useAbsolutePath: false);

            // Assert
            Assert.Equal(Path.Combine("Tests", "TestClass.cs"), result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
