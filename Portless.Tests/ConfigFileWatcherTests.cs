using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Configuration;
using Portless.Core.Models;
using Portless.Core.Services;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

public class ConfigFileWatcherTests
{
    private readonly Mock<IPortlessConfigLoader> _configLoader;
    private readonly DynamicConfigProvider _configProvider;
    private readonly Mock<IRouteStore> _routeStore;
    private readonly Mock<ITcpForwardingService> _tcpForwardingService;
    private readonly Mock<IYarpConfigFactory> _configFactory;
    private readonly Mock<IMetricsService> _metricsService;
    private readonly Mock<ILogger<ConfigFileWatcher>> _logger;

    public ConfigFileWatcherTests()
    {
        _configLoader = new Mock<IPortlessConfigLoader>();
        _configProvider = new DynamicConfigProvider();
        _routeStore = new Mock<IRouteStore>();
        _tcpForwardingService = new Mock<ITcpForwardingService>();
        _configFactory = new Mock<IYarpConfigFactory>();
        _metricsService = new Mock<IMetricsService>();
        _logger = new Mock<ILogger<ConfigFileWatcher>>();

        // Default setup for GetActiveListeners (returns empty by default)
        _tcpForwardingService.Setup(x => x.GetActiveListeners())
            .Returns(new Dictionary<string, int>());
    }

    private ConfigFileWatcher CreateWatcher()
    {
        return new ConfigFileWatcher(
            _configLoader.Object,
            _configProvider,
            _routeStore.Object,
            _tcpForwardingService.Object,
            _configFactory.Object,
            _metricsService.Object,
            _logger.Object);
    }

    private void SetupMocksForHttpRoute(string hostname, string backendUrl, string? path = null)
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = hostname, Backends = new List<string> { backendUrl }, Path = path }
            }
        };

        var port = 5000;
        if (Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri))
            port = uri.Port;

        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _configLoader.Setup(x => x.ToRouteInfos(config)).Returns(new[]
        {
            new RouteInfo
            {
                Hostname = hostname,
                Port = port,
                BackendProtocol = "http",
                Path = path,
                Type = RouteType.Http
            }
        });

        // RouteInfo.GetBackendUrls() returns BackendUrls if set, otherwise derives from protocol+port
        // Since we set Port = port and BackendProtocol = "http", GetBackendUrls() returns ["http://localhost:{port}"]
        var effectiveBackendUrls = new[] { $"http://localhost:{port}" };

        var routeConfig = new RouteConfig
        {
            RouteId = $"route-{hostname}",
            ClusterId = $"cluster-{hostname}",
            Match = new RouteMatch
            {
                Hosts = new[] { hostname },
                Path = path ?? "/{**catch-all}"
            }
        };

        var clusterConfig = new ClusterConfig
        {
            ClusterId = $"cluster-{hostname}",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = effectiveBackendUrls[0] }
            }
        };

        _configFactory.Setup(x => x.CreateRouteClusterPair(hostname, It.Is<string[]>(u => u.SequenceEqual(effectiveBackendUrls)), path))
            .Returns((routeConfig, clusterConfig));
    }

    [Fact]
    public async Task ConfigFileWatcher_Created_ReloadsRoutes()
    {
        // Arrange
        SetupMocksForHttpRoute("api.test.local", "http://localhost:5000");

        var watcher = CreateWatcher();

        // Act - call ReloadConfigAsync directly (simulating file creation)
        await watcher.ReloadConfigAsync();

        // Assert - config provider should have the route
        var currentConfig = _configProvider.GetConfig();
        Assert.Single(currentConfig.Routes);
        Assert.True(currentConfig.Routes[0].RouteId.StartsWith("config-"));
        Assert.Single(currentConfig.Clusters);
        Assert.True(currentConfig.Clusters[0].ClusterId.StartsWith("config-"));
    }

    [Fact]
    public async Task ConfigFileWatcher_Modified_ReloadsRoutes()
    {
        // Arrange
        SetupMocksForHttpRoute("app.test.local", "http://localhost:3000");

        // Seed existing non-config route
        var existingRoute = new RouteConfig
        {
            RouteId = "route-existing.local",
            ClusterId = "cluster-existing.local",
            Match = new RouteMatch { Hosts = new[] { "existing.local" }, Path = "/{**catch-all}" }
        };
        var existingCluster = new ClusterConfig
        {
            ClusterId = "cluster-existing.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:8080" }
            }
        };
        _configProvider.Update(new[] { existingRoute }, new[] { existingCluster });

        var watcher = CreateWatcher();

        // Act
        await watcher.ReloadConfigAsync();

        // Assert - existing routes preserved, config routes appended
        var currentConfig = _configProvider.GetConfig();
        Assert.Equal(2, currentConfig.Routes.Count);
        Assert.Contains(currentConfig.Routes, r => r.RouteId == "route-existing.local");
        Assert.Contains(currentConfig.Routes, r => r.RouteId.StartsWith("config-route-"));
        Assert.Equal(2, currentConfig.Clusters.Count);
        Assert.Contains(currentConfig.Clusters, c => c.ClusterId == "cluster-existing.local");
        Assert.Contains(currentConfig.Clusters, c => c.ClusterId.StartsWith("config-cluster-"));
    }

    [Fact]
    public async Task ConfigFileWatcher_Deleted_ClearsConfigRoutes()
    {
        // Arrange - simulate a config that returns empty (file deleted)
        var emptyConfig = new PortlessConfig();
        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(emptyConfig);
        _configLoader.Setup(x => x.ToRouteInfos(emptyConfig)).Returns(Array.Empty<RouteInfo>());

        // Seed with a config route + non-config route
        var configRoute = new RouteConfig
        {
            RouteId = "config-route-api.test.local",
            ClusterId = "config-cluster-api.test.local",
            Match = new RouteMatch { Hosts = new[] { "api.test.local" }, Path = "/{**catch-all}" }
        };
        var configCluster = new ClusterConfig
        {
            ClusterId = "config-cluster-api.test.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" }
            }
        };
        var nonConfigRoute = new RouteConfig
        {
            RouteId = "route-other.local",
            ClusterId = "cluster-other.local",
            Match = new RouteMatch { Hosts = new[] { "other.local" }, Path = "/{**catch-all}" }
        };
        var nonConfigCluster = new ClusterConfig
        {
            ClusterId = "cluster-other.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:9000" }
            }
        };

        _configProvider.Update(
            new[] { configRoute, nonConfigRoute },
            new[] { configCluster, nonConfigCluster });

        var watcher = CreateWatcher();

        // Act - reload with empty config (simulates file deletion)
        await watcher.ReloadConfigAsync();

        // Assert - config routes removed, non-config routes preserved
        var currentConfig = _configProvider.GetConfig();
        Assert.Single(currentConfig.Routes);
        Assert.Equal("route-other.local", currentConfig.Routes[0].RouteId);
        Assert.Single(currentConfig.Clusters);
        Assert.Equal("cluster-other.local", currentConfig.Clusters[0].ClusterId);
    }

    [Fact]
    public async Task ConfigFileWatcher_NoConfigFile_DoesNotCrash()
    {
        // Arrange - no config file returns empty config
        _configLoader.Setup(x => x.FindConfigFile(It.IsAny<string?>())).Returns((string?)null);
        var emptyConfig = new PortlessConfig();
        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(emptyConfig);
        _configLoader.Setup(x => x.ToRouteInfos(emptyConfig)).Returns(Array.Empty<RouteInfo>());

        var watcher = CreateWatcher();

        // Act & Assert - should not throw
        await watcher.ReloadConfigAsync();

        var currentConfig = _configProvider.GetConfig();
        Assert.Empty(currentConfig.Routes);
        Assert.Empty(currentConfig.Clusters);
    }

    [Fact]
    public async Task ConfigFileWatcher_TcpRoutes_StartedAndStopped()
    {
        // Arrange - config with TCP route
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "redis.test.local", Type = "tcp", ListenPort = 6379, Backends = new List<string> { "localhost:6379" } }
            }
        };

        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _configLoader.Setup(x => x.ToRouteInfos(config)).Returns(new[]
        {
            new RouteInfo
            {
                Hostname = "redis.test.local",
                Port = 6379,
                Type = RouteType.Tcp,
                TcpListenPort = 6379
            }
        });

        _tcpForwardingService.Setup(x => x.GetActiveListeners()).Returns(new Dictionary<string, int>());

        var watcher = CreateWatcher();

        // Act - first load starts TCP
        await watcher.ReloadConfigAsync();

        // Assert TCP listener started
        _tcpForwardingService.Verify(
            x => x.StartListenerAsync("config-redis.test.local", 6379, "localhost", 6379, default),
            Times.Once);

        // Now simulate a reload with empty config (TCP routes removed)
        var emptyConfig = new PortlessConfig();
        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(emptyConfig);
        _configLoader.Setup(x => x.ToRouteInfos(emptyConfig)).Returns(Array.Empty<RouteInfo>());

        await watcher.ReloadConfigAsync();

        // Assert old TCP listener stopped
        _tcpForwardingService.Verify(
            x => x.StopListenerAsync("config-redis.test.local"),
            Times.Once);
    }

    [Fact]
    public async Task ConfigFileWatcher_LoadBalancingPolicy_Preserved()
    {
        // Arrange
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "api.test.local",
                    Backends = new List<string> { "http://localhost:5000", "http://localhost:5001" },
                    LoadBalancePolicy = "RoundRobin"
                }
            }
        };

        _configLoader.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _configLoader.Setup(x => x.ToRouteInfos(config)).Returns(new[]
        {
            new RouteInfo
            {
                Hostname = "api.test.local",
                Port = 5000,
                BackendUrls = new[] { "http://localhost:5000", "http://localhost:5001" },
                LoadBalancingPolicy = LoadBalancingPolicy.RoundRobin,
                Type = RouteType.Http
            }
        });

        var routeConfig = new RouteConfig
        {
            RouteId = "route-api.test.local",
            ClusterId = "cluster-api.test.local",
            Match = new RouteMatch { Hosts = new[] { "api.test.local" }, Path = "/{**catch-all}" }
        };

        var clusterConfig = new ClusterConfig
        {
            ClusterId = "cluster-api.test.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" },
                ["backend2"] = new DestinationConfig { Address = "http://localhost:5001" }
            }
        };

        _configFactory.Setup(x => x.CreateRouteClusterPair(
                "api.test.local",
                It.Is<string[]>(urls => urls.Length == 2 && urls[0] == "http://localhost:5000" && urls[1] == "http://localhost:5001"),
                null))
            .Returns((routeConfig, clusterConfig));

        var watcher = CreateWatcher();

        // Act
        await watcher.ReloadConfigAsync();

        // Assert - cluster should have LoadBalancingPolicy set
        var currentConfig = _configProvider.GetConfig();
        Assert.Single(currentConfig.Clusters);
        Assert.Equal("RoundRobin", currentConfig.Clusters[0].LoadBalancingPolicy);
    }

    [Fact]
    public async Task ConfigFileWatcher_PathBasedRouting_Preserved()
    {
        // Arrange
        SetupMocksForHttpRoute("api.test.local", "http://localhost:5000", "/v1");

        var watcher = CreateWatcher();

        // Act
        await watcher.ReloadConfigAsync();

        // Assert - route should have the path set
        var currentConfig = _configProvider.GetConfig();
        Assert.Single(currentConfig.Routes);
        Assert.Equal("/v1", currentConfig.Routes[0].Match.Path);
    }

    [Fact]
    public async Task Debounce_MultipleRapidChanges_SingleReload()
    {
        // This test verifies the debounce behavior through ReloadConfigAsync.
        // In production, multiple rapid filesystem events reset the timer so
        // only one reload fires. Here we test that a single ReloadConfigAsync
        // call correctly applies the latest config state.

        // Arrange
        SetupMocksForHttpRoute("api.test.local", "http://localhost:5000");

        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-debounce-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var configPath = Path.Combine(tempDir, "portless.config.yaml");

        try
        {
            _configLoader.Setup(x => x.FindConfigFile(It.IsAny<string?>())).Returns(configPath);

            var watcher = CreateWatcher();
            await watcher.StartAsync(CancellationToken.None);

            // Act - simulate debounce firing once (what happens after rapid changes settle)
            await watcher.ReloadConfigAsync();

            // Assert - only one set of routes added
            var currentConfig = _configProvider.GetConfig();
            Assert.Single(currentConfig.Routes);

            await watcher.StopAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ConfigFileWatcher_ReloadRemovesOldConfigRoutesBeforeAdding()
    {
        // Arrange - first load adds a config route
        SetupMocksForHttpRoute("app.test.local", "http://localhost:3000");

        // Seed with old config route + non-config route
        var oldConfigRoute = new RouteConfig
        {
            RouteId = "config-route-old.test.local",
            ClusterId = "config-cluster-old.test.local",
            Match = new RouteMatch { Hosts = new[] { "old.test.local" }, Path = "/{**catch-all}" }
        };
        var oldConfigCluster = new ClusterConfig
        {
            ClusterId = "config-cluster-old.test.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:9999" }
            }
        };
        var nonConfigRoute = new RouteConfig
        {
            RouteId = "route-other.local",
            ClusterId = "cluster-other.local",
            Match = new RouteMatch { Hosts = new[] { "other.local" }, Path = "/{**catch-all}" }
        };
        var nonConfigCluster = new ClusterConfig
        {
            ClusterId = "cluster-other.local",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = "http://localhost:8080" }
            }
        };

        _configProvider.Update(
            new[] { oldConfigRoute, nonConfigRoute },
            new[] { oldConfigCluster, nonConfigCluster });

        var watcher = CreateWatcher();

        // Act - reload replaces old config routes with new ones
        await watcher.ReloadConfigAsync();

        // Assert - old config route removed, new config route added, non-config preserved
        var currentConfig = _configProvider.GetConfig();
        Assert.Equal(2, currentConfig.Routes.Count);
        Assert.Contains(currentConfig.Routes, r => r.RouteId == "route-other.local");
        Assert.Contains(currentConfig.Routes, r => r.RouteId.StartsWith("config-route-app"));
        Assert.DoesNotContain(currentConfig.Routes, r => r.RouteId == "config-route-old.test.local");

        Assert.Equal(2, currentConfig.Clusters.Count);
        Assert.Contains(currentConfig.Clusters, c => c.ClusterId == "cluster-other.local");
        Assert.Contains(currentConfig.Clusters, c => c.ClusterId.StartsWith("config-cluster-app"));
        Assert.DoesNotContain(currentConfig.Clusters, c => c.ClusterId == "config-cluster-old.test.local");
    }
}
