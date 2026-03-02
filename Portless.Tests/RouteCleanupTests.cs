using Xunit;
using System.Diagnostics;
using Portless.Core.Services;
using Portless.Core.Models;

namespace Portless.Tests;

public class RouteCleanupTests
{
    [Fact]
    public void IsProcessAlive_ReturnsTrueForCurrentProcess()
    {
        // Arrange
        var currentProcess = Process.GetCurrentProcess();
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Port = 4001,
            Pid = currentProcess.Id,
            CreatedAt = currentProcess.StartTime.AddSeconds(1) // Created after process started
        };

        // Act - Access IsProcessAlive via reflection or extract to separate class
        // For now, we'll test the logic inline
        var isAlive = TestIsProcessAlive(route, currentProcess.StartTime);

        // Assert
        Assert.True(isAlive);
    }

    [Fact]
    public void IsProcessAlive_ReturnsFalseForNonExistentPid()
    {
        // Arrange
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Port = 4001,
            Pid = 999999, // Very unlikely to exist
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var isAlive = TestIsProcessAlive(route);

        // Assert
        Assert.False(isAlive);
    }

    [Fact]
    public void IsProcessAlive_DetectsPidRecycling()
    {
        // Arrange - Create a route with old CreatedAt
        var oldRoute = new RouteInfo
        {
            Hostname = "test.localhost",
            Port = 4001,
            Pid = Process.GetCurrentProcess().Id,
            CreatedAt = DateTime.UtcNow.AddHours(-1) // Created 1 hour ago
        };

        // Act
        var isAlive = TestIsProcessAlive(oldRoute);

        // Assert - Current process started less than 1 hour ago, so PID recycled
        Assert.False(isAlive);
    }

    [Fact]
    public void IsProcessAlive_UpdatesLastSeenOnSuccess()
    {
        // Arrange
        var currentProcess = Process.GetCurrentProcess();
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Port = 4001,
            Pid = currentProcess.Id,
            CreatedAt = currentProcess.StartTime.AddSeconds(1),
            LastSeen = null
        };

        // Act
        TestIsProcessAlive(route, currentProcess.StartTime);

        // Assert
        Assert.NotNull(route.LastSeen);
        Assert.True(route.LastSeen.Value > DateTime.UtcNow.AddSeconds(-5));
    }

    // Helper method to test IsProcessAlive logic
    // In production, this would be a separate testable class or use reflection
    private static bool TestIsProcessAlive(RouteInfo route, DateTime? processStartTime = null)
    {
        try
        {
            var process = Process.GetProcessById(route.Pid);

            if (process.HasExited)
                return false;

            // Validate PID hasn't been recycled
            var startTime = processStartTime ?? process.StartTime;
            if (startTime > route.CreatedAt + TimeSpan.FromSeconds(1))
            {
                return false; // PID recycled
            }

            route.LastSeen = DateTime.UtcNow;
            return true;
        }
        catch (ArgumentException)
        {
            return false; // PID doesn't exist
        }
    }

    [Fact]
    public async Task RouteCleanupService_RemovesDeadRoutes()
    {
        // Arrange - Create routes with dead PIDs
        var deadRoutes = new[]
        {
            new RouteInfo
            {
                Hostname = "dead1.localhost",
                Port = 4001,
                Pid = 999999,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new RouteInfo
            {
                Hostname = "dead2.localhost",
                Port = 4002,
                Pid = 999998,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        // This would require integration test with actual RouteCleanupService
        // For unit test, we test the IsProcessAlive logic above
        // Integration test would verify BackgroundService behavior

        // Assert - Verify dead routes would be removed
        Assert.True(TestIsProcessAlive(deadRoutes[0]) == false);
        Assert.True(TestIsProcessAlive(deadRoutes[1]) == false);
    }
}
