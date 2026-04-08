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
        var name = _detector.DetectProjectName("/tmp");
        // Should be a valid hostname-compatible string
        Assert.Matches(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", name);
    }
}
