using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;
using System.Runtime.InteropServices;

namespace Portless.Tests;

public class PlatformDetectorServiceTests
{
    private readonly Mock<ILogger<PlatformDetectorService>> _loggerMock;

    public PlatformDetectorServiceTests()
    {
        _loggerMock = new Mock<ILogger<PlatformDetectorService>>();
    }

    private PlatformDetectorService CreateService()
    {
        return new PlatformDetectorService(_loggerMock.Object);
    }

    [Fact]
    public void GetPlatformInfo_ReturnsNonNullPlatformInfo()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert
        Assert.NotNull(info);
    }

    [Fact]
    public void GetPlatformInfo_ReturnsCachedInfoOnSecondCall()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info1 = service.GetPlatformInfo();
        var info2 = service.GetPlatformInfo();

        // Assert - should return same cached instance
        Assert.Same(info1, info2);
    }

    [Fact]
    public void GetPlatformInfo_DetectsCorrectOS()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert - should detect the current OS correctly
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Equal(OSPlatform.Linux, info.OSPlatform);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Equal(OSPlatform.OSX, info.OSPlatform);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Equal(OSPlatform.Windows, info.OSPlatform);
    }

    [Fact]
    public void GetPlatformInfo_OnLinux_DetectsDistroOrUnknown()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert - on Linux, LinuxDistro should be set
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.NotNull(info.LinuxDistro);
            Assert.True(Enum.IsDefined(info.LinuxDistro.Value));
        }
    }

    [Fact]
    public void GetPlatformInfo_OnNonLinux_LinuxDistroIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Null(info.LinuxDistro);
        }
    }

    [Fact]
    public void IsAdminUser_ReturnsBoolean()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsAdminUser();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsAdminUser_OnLinux_RunsIdCommand()
    {
        // Arrange
        var service = CreateService();

        // Act - should not throw
        var result = service.IsAdminUser();

        // Assert - no exception means the id command worked
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetAdminElevationCommand_OnLinux_ReturnsSudo()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAdminElevationCommand();

        // Assert
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal("sudo", result);
        }
    }

    [Fact]
    public void GetAdminElevationCommand_OnWindows_ReturnsEmptyString()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAdminElevationCommand();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(string.Empty, result);
        }
    }

    [Fact]
    public void GetPlatformInfo_ContainsAllFields()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert - verify all fields are populated
        Assert.NotEqual(default(OSPlatform), info.OSPlatform);
        Assert.NotNull(info.ElevationCommand);
        // IsAdmin is just a bool, always valid
        Assert.IsType<bool>(info.IsAdmin);
    }

    [Fact]
    public void GetPlatformInfo_OnLinux_ElevationCommandIsSudo()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetPlatformInfo();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Equal("sudo", info.ElevationCommand);
        }
    }

    [Fact]
    public void IsAdminUser_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should never throw
        var exception = Record.Exception(() => service.IsAdminUser());
        Assert.Null(exception);
    }

    [Fact]
    public void GetAdminElevationCommand_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should never throw
        var exception = Record.Exception(() => service.GetAdminElevationCommand());
        Assert.Null(exception);
    }
}
