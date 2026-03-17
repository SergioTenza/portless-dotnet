// Portless.Tests/PlatformDetectionTests.cs
using Portless.Core.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Portless.Tests;

public class PlatformDetectionTests
{
    [Fact]
    public void GetPlatformInfo_ReturnsCurrentPlatform()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<PlatformDetectorService>();
        var detector = new PlatformDetectorService(logger);

        // Act
        var info = detector.GetPlatformInfo();

        // Assert
        Assert.NotNull(info);
        Assert.NotNull(info.OSPlatform);
    }

    [Fact]
    public void IsAdminUser_ReturnsBoolean()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<PlatformDetectorService>();
        var detector = new PlatformDetectorService(logger);

        // Act
        var isAdmin = detector.IsAdminUser();

        // Assert
        Assert.IsType<bool>(isAdmin);
    }
}
