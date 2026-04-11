using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProjectNameDetectorExtendedTests
{
    private readonly ProjectNameDetector _detector = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-pname-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void DetectProjectName_FromCsprojAssemblyName()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "App.csproj"),
                "<Project><PropertyGroup><AssemblyName>MyCustomApp</AssemblyName></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("mycustomapp", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FromCsprojPackageId()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Lib.csproj"),
                "<Project><PropertyGroup><PackageId>My.Package</PackageId></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("my-package", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FromCsprojFileName()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "SimpleApp.csproj"),
                "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("simpleapp", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FromCsprojInSubdirectory()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var subDir = Path.Combine(tmpDir, "src");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "SubProject.csproj"),
                "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("subproject", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FromDirectoryBuildProps()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Directory.Build.props"),
                "<Project><PropertyGroup><ProjectName>MyPropject</ProjectName></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("mypropject", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FromGitRootDirectory()
    {
        var tmpDir = CreateTempDir();
        try
        {
            // Create a git repo structure
            var gitDir = Path.Combine(tmpDir, "my-repo");
            Directory.CreateDirectory(gitDir);
            Directory.CreateDirectory(Path.Combine(gitDir, ".git"));
            var workDir = Path.Combine(gitDir, "src");
            Directory.CreateDirectory(workDir);

            var name = _detector.DetectProjectName(workDir);
            Assert.Equal("my-repo", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_FallsBackToDirectoryName()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var projectDir = Path.Combine(tmpDir, "My Cool Project");
            Directory.CreateDirectory(projectDir);

            var name = _detector.DetectProjectName(projectDir);
            Assert.Equal("my-cool-project", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_SanitizesDots()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var projectDir = Path.Combine(tmpDir, "My.Project.Name");
            Directory.CreateDirectory(projectDir);

            var name = _detector.DetectProjectName(projectDir);
            Assert.Equal("my-project-name", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_SanitizesSpecialChars()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var projectDir = Path.Combine(tmpDir, "My@Project#2024!");
            Directory.CreateDirectory(projectDir);

            var name = _detector.DetectProjectName(projectDir);
            // Special chars should be removed
            Assert.DoesNotContain("@", name);
            Assert.DoesNotContain("#", name);
            Assert.DoesNotContain("!", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_RemovesDllSuffix()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "MyApp.dll.csproj"),
                "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("myapp", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_RemovesExeSuffix()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "MyApp.exe.csproj"),
                "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("myapp", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_CsprojPriorityOverGitRoot()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var gitDir = Path.Combine(tmpDir, "git-repo");
            Directory.CreateDirectory(gitDir);
            Directory.CreateDirectory(Path.Combine(gitDir, ".git"));
            File.WriteAllText(Path.Combine(gitDir, "App.csproj"),
                "<Project><PropertyGroup><AssemblyName>PriorityApp</AssemblyName></PropertyGroup></Project>");

            var name = _detector.DetectProjectName(gitDir);
            Assert.Equal("priorityapp", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectProjectName_MultipleCsprojs_WebProjectPriority()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Lib.csproj"),
                "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            File.WriteAllText(Path.Combine(tmpDir, "Web.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

            var name = _detector.DetectProjectName(tmpDir);
            Assert.Equal("web", name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }
}
