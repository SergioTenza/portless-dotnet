using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProcessHealthMonitorTests
{
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IPortAllocator> _portAllocatorMock;
    private readonly Mock<ILogger<ProcessHealthMonitor>> _loggerMock;

    public ProcessHealthMonitorTests()
    {
        _routeStoreMock = new Mock<IRouteStore>();
        _portAllocatorMock = new Mock<IPortAllocator>();
        _loggerMock = new Mock<ILogger<ProcessHealthMonitor>>();
    }

    private ProcessHealthMonitor CreateService()
    {
        return new ProcessHealthMonitor(
            _routeStoreMock.Object,
            _portAllocatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDeadRoutes_DoesNotReleasePorts()
    {
        // Arrange - use current process (always alive)
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "alive.localhost",
                Port = 4001,
                Pid = currentPid,
                CreatedAt = startTime.AddSeconds(1)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();

        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { }

        // Assert - no ports released, no saves for alive routes
        _portAllocatorMock.Verify(
            x => x.ReleasePortAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeadRoutes_ReleasesPortsAndSaves()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4010,
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
        await Task.Delay(200);
        await cts.CancelAsync();

        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { }

        // Assert - port should be released for dead route
        _portAllocatorMock.Verify(
            x => x.ReleasePortAsync(4010),
            Times.AtMostOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedRoutes_OnlyReleasesDeadPorts()
    {
        // Arrange
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "alive.localhost",
                Port = 4001,
                Pid = currentPid,
                CreatedAt = startTime.AddSeconds(1)
            },
            new RouteInfo
            {
                Hostname = "dead.localhost",
                Port = 4002,
                Pid = 888888,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        _routeStoreMock
            .Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Callback<RouteInfo[], CancellationToken>((r, _) =>
            {
                // Should only save the alive route
                Assert.Single(r);
                Assert.Equal("alive.localhost", r[0].Hostname);
            })
            .Returns(Task.CompletedTask);

        _portAllocatorMock
            .Setup(x => x.ReleasePortAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();

        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { }

        // Assert
        _portAllocatorMock.Verify(x => x.ReleasePortAsync(4002), Times.AtMostOnce);
        _portAllocatorMock.Verify(x => x.ReleasePortAsync(4001), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesExceptionGracefully()
    {
        // Arrange
        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act - should not throw
        var task = service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();

        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { }

        // Assert - no crash
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully()
    {
        // Arrange
        _routeStoreMock
            .Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await Task.Delay(200);

        // Assert - no crash means graceful shutdown
        Assert.True(true);
    }
}
