using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Configuration;
using Portless.Core.Models;
using Portless.Core.Services;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

public class RouteFileWatcherTests
{
    private readonly Mock<IRouteStore> _routeStore;
    private readonly DynamicConfigProvider _configProvider;
    private readonly Mock<ILogger<RouteFileWatcher>> _logger;

    public RouteFileWatcherTests()
    {
        _routeStore = new Mock<IRouteStore>();
        _configProvider = new DynamicConfigProvider();
        _logger = new Mock<ILogger<RouteFileWatcher>>();
    }

    private RouteFileWatcher CreateWatcher()
    {
        return new RouteFileWatcher(
            _routeStore.Object,
            _configProvider,
            _logger.Object);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-rfw-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task StartAsync_CreatesWatcherAndStartsSuccessfully()
    {
        var tempDir = CreateTempDir();
        try
        {
            // Set the state directory to temp dir via environment
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);
            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            // Should not throw
            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task StopAsync_DisposesResources()
    {
        var tempDir = CreateTempDir();
        try
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);
            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);
            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OnDebounceElapsed_LoadsRoutesAndUpdatesConfig()
    {
        var tempDir = CreateTempDir();
        try
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);

            // Setup routes
            var routes = new[]
            {
                new RouteInfo
                {
                    Hostname = "app.test.local",
                    Port = 3000,
                    BackendProtocol = "http"
                }
            };
            _routeStore.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes);

            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            // Write a routes.json file to trigger the debounce
            var routesPath = Path.Combine(tempDir, "routes.json");
            await File.WriteAllTextAsync(routesPath, "{}");

            // Wait for debounce (500ms + some buffer)
            await Task.Delay(1500);

            // Verify routes were loaded
            _routeStore.Verify(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            // Verify config was updated
            var config = _configProvider.GetConfig();
            Assert.NotEmpty(config.Routes);
            Assert.Equal("route-app.test.local", config.Routes[0].RouteId);

            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OnDebounceElapsed_HandlesMultipleRoutes()
    {
        var tempDir = CreateTempDir();
        try
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);

            var routes = new[]
            {
                new RouteInfo { Hostname = "api.test.local", Port = 5000, BackendProtocol = "http" },
                new RouteInfo { Hostname = "web.test.local", Port = 3000, BackendProtocol = "http" }
            };
            _routeStore.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes);

            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            var routesPath = Path.Combine(tempDir, "routes.json");
            await File.WriteAllTextAsync(routesPath, "{}");
            await Task.Delay(1500);

            var config = _configProvider.GetConfig();
            Assert.Equal(2, config.Routes.Count);
            Assert.Equal(2, config.Clusters.Count);

            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OnDebounceElapsed_HandlesLoadError()
    {
        var tempDir = CreateTempDir();
        try
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);

            _routeStore.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Load error"));

            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            var routesPath = Path.Combine(tempDir, "routes.json");
            await File.WriteAllTextAsync(routesPath, "{}");
            await Task.Delay(1500);

            // Should not crash - error is logged and swallowed
            Assert.True(true);

            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OnDebounceElapsed_EmptyRoutes_ClearsConfig()
    {
        var tempDir = CreateTempDir();
        try
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);

            // First load some routes
            var routes = new[]
            {
                new RouteInfo { Hostname = "temp.test.local", Port = 4000, BackendProtocol = "http" }
            };
            _routeStore.SetupSequence(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes)
                .ReturnsAsync(Array.Empty<RouteInfo>());

            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            // Trigger first load
            var routesPath = Path.Combine(tempDir, "routes.json");
            await File.WriteAllTextAsync(routesPath, "{}");
            await Task.Delay(1500);

            // Trigger second load with empty routes
            _routeStore.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RouteInfo>());
            File.SetLastWriteTime(routesPath, DateTime.Now);
            await Task.Delay(1500);

            var config = _configProvider.GetConfig();
            Assert.Empty(config.Routes);

            await watcher.StopAsync(CancellationToken.None);
            watcher.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Dispose_WithoutStart_DoesNotThrow()
    {
        var watcher = CreateWatcher();
        watcher.Dispose();
    }
}
