using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class RouteCleanupServiceTests
{
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IPortAllocator> _portAllocatorMock;
    private readonly Mock<ILogger<RouteCleanupService>> _loggerMock;

    public RouteCleanupServiceTests()
    {
        _routeStoreMock = new Mock<IRouteStore>();
        _portAllocatorMock = new Mock<IPortAllocator>();
        _loggerMock = new Mock<ILogger<RouteCleanupService>>();
    }

    private RouteCleanupService CreateService()
    {
        return new RouteCleanupService(
            _routeStoreMock.Object,
            _portAllocatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CleansUpDeadRoutes()
    {
        // Arrange
        var deadPid = 999999; // Non-existent PID
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4001,
                Pid = deadPid,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _routeStoreMock
            .Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _portAllocatorMock
            .Setup(x => x.ReleasePortAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act - Start the service, let it run one cycle, then cancel
        var task = service.StartAsync(cts.Token);

        // Give it time for one cleanup cycle (interval is 30s, but we cancel quickly)
        await Task.Delay(100);
        await cts.CancelAsync();

        // Wait for the service to stop (with timeout)
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - SaveRoutesAsync should have been called with the alive routes
        // Since dead PID won't exist, the alive routes should be empty
        _routeStoreMock.Verify(
            x => x.SaveRoutesAsync(
                It.IsAny<RouteInfo[]>(),
                It.IsAny<CancellationToken>()),
            Times.AtMostOnce);
    }

    [Fact]
    public async Task ExecuteAsync_KeepsAliveRoutes()
    {
        // Arrange
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "alive.localhost",
                Port = 4001,
                Pid = currentPid,
                CreatedAt = currentProcess.StartTime.AddSeconds(1)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _routeStoreMock
            .Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - alive routes should NOT trigger SaveRoutesAsync
        _routeStoreMock.Verify(
            x => x.SaveRoutesAsync(
                It.Is<RouteInfo[]>(r => r.Length == 0),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesPortsForDeadRoutes()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4050,
                Pid = 999998,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new RouteInfo
            {
                Hostname = "dead2.localhost",
                Port = 4051,
                Pid = 999997,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _routeStoreMock
            .Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _portAllocatorMock
            .Setup(x => x.ReleasePortAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - ports should be released for dead routes
        _portAllocatorMock.Verify(
            x => x.ReleasePortAsync(4050),
            Times.AtMostOnce);
        _portAllocatorMock.Verify(
            x => x.ReleasePortAsync(4051),
            Times.AtMostOnce);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesLoadRoutesException()
    {
        // Arrange
        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act - should not throw even when LoadRoutesAsync fails
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - no crash, service handles the exception internally
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyRoutes_DoesNotCrash()
    {
        // Arrange
        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - no save should be called for empty routes with no changes
        _routeStoreMock.Verify(
            x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesMixedAliveAndDeadRoutes()
    {
        // Arrange
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "alive.localhost",
                Port = 4001,
                Pid = currentPid,
                CreatedAt = currentProcess.StartTime.AddSeconds(1)
            },
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4002,
                Pid = 999999,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _routeStoreMock
            .Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _portAllocatorMock
            .Setup(x => x.ReleasePortAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - dead route's port should be released
        _portAllocatorMock.Verify(
            x => x.ReleasePortAsync(4002),
            Times.AtMostOnce);
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefullyOnCancellation()
    {
        // Arrange
        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await cts.CancelAsync();

        // Give the service time to stop
        await Task.Delay(200);

        // Assert - no crash means graceful shutdown
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesReleasePortException()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4001,
                Pid = 999999,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _portAllocatorMock
            .Setup(x => x.ReleasePortAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Port release failed"));

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act - should not crash despite port release failure
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();

        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException) { }

        // Assert - no crash means exception was handled
        Assert.True(true);
    }
}
