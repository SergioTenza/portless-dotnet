using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class FrameworkDetectorTests
{
    private readonly FrameworkDetector _detector = new();

    [Fact]
    public void Detect_ReturnsNull_ForEmptyDir()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-empty-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var result = _detector.Detect(tmpDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void Detect_ReturnsGo_ForGoMod()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-go-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "go.mod"), "module example.com/myapp\ngo 1.21");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("go", result.Name);
            Assert.Equal("Go", result.DisplayName);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void Detect_ReturnsRust_ForCargoToml()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-rust-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Cargo.toml"), "[package]\nname = \"myapp\"");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("rust", result.Name);
            Assert.Equal("Rust", result.DisplayName);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void Detect_ReturnsPython_ForRequirementsTxt()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-py-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "requirements.txt"), "flask==3.0");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("python", result.Name);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void Detect_ReturnsNpm_ForPackageJsonWithStart()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-npm-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "package.json"), "{\"name\":\"test\",\"scripts\":{\"start\":\"node index.js\"}}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("npm", result.Name);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void Detect_ViteOverNpm_WhenBothPresent()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-vite-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "package.json"), "{\"name\":\"test\",\"scripts\":{\"start\":\"vite\"}}");
            File.WriteAllText(Path.Combine(tmpDir, "vite.config.js"), "export default {}");
            var result = _detector.Detect(tmpDir);
            Assert.NotNull(result);
            Assert.Equal("vite", result.Name);
            Assert.Contains("--strictPort", result.InjectedFlags);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }
}
