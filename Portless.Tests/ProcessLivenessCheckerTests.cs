using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProcessLivenessCheckerTests
{
    private readonly Mock<ILogger<ProcessLivenessChecker>> _loggerMock;
    private readonly ProcessLivenessChecker _checker;

    public ProcessLivenessCheckerTests()
    {
        _loggerMock = new Mock<ILogger<ProcessLivenessChecker>>();
        _checker = new ProcessLivenessChecker(_loggerMock.Object);
    }

    [Fact]
    public void IsAlive_ReturnsFalse_WhenPidIsZero()
    {
        // Arrange
        var route = new RouteInfo { Hostname = "test.localhost", Pid = 0, Port = 4042 };

        // Act
        var result = _checker.IsAlive(route);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlive_ReturnsFalse_WhenPidIsNegative()
    {
        // Arrange
        var route = new RouteInfo { Hostname = "test.localhost", Pid = -1, Port = 4042 };

        // Act
        var result = _checker.IsAlive(route);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlive_ReturnsFalse_WhenPidDoesNotExist()
    {
        // Arrange - use a very high PID that almost certainly doesn't exist
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Pid = 99999999,
            Port = 4042,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = _checker.IsAlive(route);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlive_ReturnsTrue_ForRunningCurrentProcess()
    {
        // Arrange
        var currentPid = Environment.ProcessId;
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Pid = currentPid,
            Port = 4042,
            CreatedAt = DateTime.UtcNow.AddSeconds(10) // Set in future so process start is before route creation
        };

        // Act
        var result = _checker.IsAlive(route);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAlive_ReturnsFalse_WhenProcessStartedAfterRouteCreation()
    {
        // Arrange - current process started way before a "route" created in the far future
        // PID recycling protection: process started AFTER route was created
        var currentPid = Environment.ProcessId;
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Pid = currentPid,
            Port = 4042,
            CreatedAt = DateTime.UtcNow.AddDays(-365) // Route was created a year ago
        };

        // Act - current process started much later than route creation,
        // so this simulates PID recycling
        var result = _checker.IsAlive(route);

        // Assert - should be false due to PID recycling check
        // The current process started more recently than a year ago, so
        // StartTime > CreatedAt + 1 second
        Assert.False(result);
    }

    [Fact]
    public void IsAlive_ReturnsFalse_ForNonExistentPid()
    {
        // Arrange
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Pid = int.MaxValue, // Extremely unlikely to exist
            Port = 4042,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = _checker.IsAlive(route);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlive_HandlesZeroPidGracefully()
    {
        // Arrange
        var route = new RouteInfo { Hostname = "test.localhost", Pid = 0 };

        // Act & Assert - should not throw
        var result = _checker.IsAlive(route);
        Assert.False(result);
    }
}
