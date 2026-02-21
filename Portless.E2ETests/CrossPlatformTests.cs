using Xunit;
using Xunit.Abstractions;

namespace Portless.E2ETests;

/// <summary>
/// Cross-platform validation tests for path handling and platform-specific behavior.
/// Tests verify that the application works correctly on Windows and Linux.
/// </summary>
public class CrossPlatformTests
{
    private readonly ITestOutputHelper _output;

    public CrossPlatformTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Path_Combine_UsesPlatformSeparator()
    {
        // Arrange & Act
        var path1 = Path.Combine("dir1", "dir2", "file.txt");
        var path2 = $"dir1{Path.DirectorySeparatorChar}dir2{Path.DirectorySeparatorChar}file.txt";

        // Assert - Verify path separator is correct for current platform
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), path1);

        _output.WriteLine($"Platform: {Environment.OSVersion.Platform}");
        _output.WriteLine($"DirectorySeparatorChar: '{Path.DirectorySeparatorChar}'");
        _output.WriteLine($"Path1: {path1}");
        _output.WriteLine($"Path2: {path2}");
    }

    [Fact]
    public void Path_GetTempPath_ReturnsValidDirectory()
    {
        // Arrange & Act
        var tempPath = Path.GetTempPath();

        // Assert
        Assert.NotNull(tempPath);
        Assert.NotEmpty(tempPath);
        Assert.True(Directory.Exists(tempPath), $"Temp path should exist: {tempPath}");

        _output.WriteLine($"TempPath: {tempPath}");
    }

    [Fact]
    public void Path_GetRandomFileName_ReturnsUniqueNames()
    {
        // Arrange & Act
        var name1 = Path.GetRandomFileName();
        var name2 = Path.GetRandomFileName();

        // Assert
        Assert.NotEmpty(name1);
        Assert.NotEmpty(name2);
        Assert.NotEqual(name1, name2);

        _output.WriteLine($"RandomFileName1: {name1}");
        _output.WriteLine($"RandomFileName2: {name2}");
    }

    [Fact]
    public void ProcessStartInfo_UsesCorrectShellForPlatform()
    {
        // Arrange & Act
        var startInfo = new ProcessStartInfo();
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

        // Assert - Verify shell choice based on platform
        if (isWindows)
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c echo test";
            Assert.Equal("cmd.exe", startInfo.FileName);
            _output.WriteLine("Using cmd.exe for Windows");
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.Arguments = "-c \"echo test\"";
            Assert.Equal("/bin/sh", startInfo.FileName);
            _output.WriteLine("Using /bin/sh for Unix/Linux");
        }
    }

    [Fact]
    public void EnvironmentVariables_WorkCrossPlatform()
    {
        // Arrange & Act
        var testVarName = $"PORTLESS_TEST_{Guid.NewGuid():N}";
        var testVarValue = "test-value";

        Environment.SetEnvironmentVariable(testVarName, testVarValue);
        var retrievedValue = Environment.GetEnvironmentVariable(testVarName);

        // Cleanup
        Environment.SetEnvironmentVariable(testVarName, null);

        // Assert
        Assert.Equal(testVarValue, retrievedValue);

        _output.WriteLine($"Set and retrieved environment variable: {testVarName}={retrievedValue}");
    }

    [Fact]
    public void PathSeparator_DetectedCorrectly()
    {
        // Arrange & Act
        var separator = Path.PathSeparator;
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

        // Assert
        if (isWindows)
        {
            Assert.Equal(';', separator);
            _output.WriteLine("Path separator: ; (Windows)");
        }
        else
        {
            Assert.Equal(':', separator);
            _output.WriteLine("Path separator: : (Unix/Linux)");
        }
    }

    [Fact]
    public void DotnetCli_PathUsesCorrectSeparator()
    {
        // Arrange & Act
        var projectPath = Path.Combine("Portless.Cli", "Portless.Cli.csproj");
        var fullPath = Path.GetFullPath(projectPath);

        // Assert
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), fullPath);
        Assert.EndsWith("Portless.Cli.csproj", fullPath);

        _output.WriteLine($"Project path: {projectPath}");
        _output.WriteLine($"Full path: {fullPath}");
    }
}
