using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class FrameworkDetectorEdgeCaseTests
{
    private readonly FrameworkDetector _detector = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-fw-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Detect_ReturnsAspNet_ForWebCsproj()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "WebApp.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("aspnet", result.Name);
            Assert.Equal("ASP.NET Core", result.DisplayName);
            Assert.Contains(result.InjectedEnvVars, v => v.Contains("ASPNETCORE_URLS"));
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsDotNet_ForNonWebCsproj()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "ConsoleApp.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("dotnet", result.Name);
            Assert.Equal(".NET", result.DisplayName);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsAspNet_ForAspNetCoreReference()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "App.csproj"),
                "<Project><ItemGroup><PackageReference Include=\"Microsoft.AspNetCore\" Version=\"8.0\" /></ItemGroup></Project>");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("aspnet", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsAspNet_ForCsprojInSubdirectory()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var subDir = Path.Combine(tmpDir, "src");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "MyWeb.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("aspnet", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsNextJs_ForNextConfig()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "next.config.js"), "module.exports = {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("nextjs", result.Name);
            Assert.Equal("Next.js", result.DisplayName);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsAstro_ForAstroConfig()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "astro.config.mjs"), "export default {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("astro", result.Name);
            Assert.Contains("--port", result.InjectedFlags);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsAngular_ForAngularJson()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "angular.json"), "{}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("angular", result.Name);
            Assert.Contains("--port", result.InjectedFlags);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsExpo_ForPackageJsonWithExpo()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "package.json"),
                "{\"name\":\"myapp\",\"dependencies\":{\"expo\":\"~50.0.0\"}}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("expo", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsReactNative_ForMetroConfig()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "metro.config.js"), "module.exports = {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("react-native", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsPython_ForPyprojectToml()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "pyproject.toml"), "[project]\nname = \"myapp\"");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("python", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsPython_ForPipfile()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Pipfile"), "[packages]\nflask = \"*\"");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("python", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsPython_ForSetupPy()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "setup.py"), "from setuptools import setup; setup()");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("python", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsNull_ForPackageJsonWithoutStart()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "package.json"),
                "{\"name\":\"myapp\",\"scripts\":{\"build\":\"tsc\"}}");
            var result = _detector.Detect(tmpDir);
            Assert.Null(result);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_DetectionPriority_ViteOverNext()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "vite.config.ts"), "export default {}");
            File.WriteAllText(Path.Combine(tmpDir, "next.config.js"), "module.exports = {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("vite", result.Name);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void Detect_ReturnsVite_ForTsConfig()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "vite.config.ts"), "export default {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("vite", result.Name);
            Assert.Contains("--strictPort", result.InjectedFlags);
            Assert.Contains("--host", result.InjectedFlags);
        }
        finally { Directory.Delete(tmpDir, true); }
    }
}
