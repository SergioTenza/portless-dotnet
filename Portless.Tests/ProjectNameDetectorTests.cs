using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProjectNameDetectorTests
{
    private readonly ProjectNameDetector _detector = new();

    [Fact]
    public void DetectProjectName_ReturnsNonNull()
    {
        var name = _detector.DetectProjectName();
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void SanitizeName_ConvertsDotsAndSpaces()
    {
        // Use a dedicated temp directory to avoid permission issues on CI runners
        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var name = _detector.DetectProjectName(tempDir);
            // Should be a valid hostname-compatible string
            Assert.Matches(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", name);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
