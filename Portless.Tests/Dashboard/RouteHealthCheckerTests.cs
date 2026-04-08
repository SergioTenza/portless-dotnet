using Microsoft.Extensions.Logging;
using Portless.Core.Models;
using Portless.Core.Services;
using Moq;

namespace Portless.Tests.Dashboard;

public sealed class RouteHealthCheckerTests : IAsyncLifetime
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"portless-health-test-{Guid.NewGuid():N}");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CheckHealthAsync_UnknownHost_ReturnsUnknown()
    {
        // Arrange
        var routeStore = new Mock<IRouteStore>();
        var logger = new Mock<ILogger<RouteHealthChecker>>();
        
        routeStore.Setup(r => r.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        
        using var checker = new RouteHealthChecker(routeStore.Object, logger.Object);
        
        // Act
        var result = await checker.CheckHealthAsync("nonexistent.localhost");
        
        // Assert
        Assert.Equal(RouteHealthStatus.Unknown, result);
    }

    [Fact]
    public async Task GetAllHealthStatuses_InitiallyEmpty()
    {
        // Arrange
        var routeStore = new Mock<IRouteStore>();
        var logger = new Mock<ILogger<RouteHealthChecker>>();
        
        routeStore.Setup(r => r.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        
        using var checker = new RouteHealthChecker(routeStore.Object, logger.Object);
        
        // Act
        var statuses = checker.GetAllHealthStatuses();
        
        // Assert
        Assert.Empty(statuses);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterCheckingUnreachableHost_ReturnsUnhealthy()
    {
        // Arrange - use a port that's definitely not listening
        var routeStore = new Mock<IRouteStore>();
        var logger = new Mock<ILogger<RouteHealthChecker>>();
        
        // Use a port in the ephemeral range that's very unlikely to be in use
        var route = new RouteInfo
        {
            Hostname = "test.localhost",
            Port = 49999,
            Type = RouteType.Http
        };
        
        routeStore.Setup(r => r.LoadRoutesAsync()).ReturnsAsync(new[] { route });
        
        using var checker = new RouteHealthChecker(routeStore.Object, logger.Object);
        
        // Use reflection to call CheckAllRoutesAsync directly to avoid the 10-second polling delay
        var method = typeof(RouteHealthChecker).GetMethod("CheckAllRoutesAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            // Get the method signature
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                var task = (Task?)method.Invoke(checker, null);
                if (task != null) await task;
            }
            else
            {
                var task = (Task?)method.Invoke(checker, new object[] { CancellationToken.None });
                if (task != null) await task;
            }
        }
        
        // Act
        var result = await checker.CheckHealthAsync("test.localhost");
        
        // Assert
        Assert.Equal(RouteHealthStatus.Unhealthy, result);
    }
}
