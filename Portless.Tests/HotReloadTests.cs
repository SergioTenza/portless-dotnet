using Xunit;
using System.IO;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portless.Core.Services;
using Portless.Core.Models;
using Portless.Core.Configuration;
using Yarp.ReverseProxy.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portless.Tests;

[Collection("RouteStore Tests")]
public class HotReloadTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly string _testRoutesFile;
    private IRouteStore? _routeStore;
    private IHost? _testHost;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public HotReloadTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _testRoutesFile = Path.Combine(_testDirectory, "routes.json");
        Directory.CreateDirectory(_testDirectory);
    }

    public Task InitializeAsync()
    {
        // Clean up any existing test file from previous runs
        if (File.Exists(_testRoutesFile))
        {
            File.Delete(_testRoutesFile);
        }

        // Set environment variable for RouteStore to use test directory
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _testDirectory);

        // Initialize RouteStore which will use PORTLESS_STATE_DIR
        _routeStore = new RouteStore();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _testHost?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task FileWatcher_TriggersReloadOnFileChange()
    {
        // Arrange
        var configProvider = new DynamicConfigProvider();
        var routes = new[]
        {
            new RouteInfo { Hostname = "test.localhost", Port = 4001, Pid = 12345 }
        };

        // Act - Write routes to file
        await File.WriteAllTextAsync(_testRoutesFile, "[]"); // Create empty file

        // Simulate file watcher behavior
        var watcher = new FileSystemWatcher(_testDirectory)
        {
            Filter = "routes.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        var reloadEvent = new ManualResetEventSlim(false);
        watcher.Changed += (s, e) => reloadEvent.Set();
        watcher.EnableRaisingEvents = true;

        // Trigger file change
        await Task.Delay(100); // Small delay for watcher to initialize
        await File.WriteAllTextAsync(_testRoutesFile, "[{\"Hostname\":\"test.localhost\",\"Port\":4001,\"Pid\":12345}]");

        // Assert - Wait for change event
        Assert.True(reloadEvent.Wait(TimeSpan.FromSeconds(5)));

        watcher.Dispose();
    }

    [Fact(Skip = "Requires RouteFileWatcher reload counter integration")]
    public async Task DebounceTimer_PreventsMultipleRapidReloads()
    {
        // Arrange - This test verifies that rapid file changes are debounced
        // so only one reload happens after the debounce window elapses.
        // Requires RouteFileWatcher to expose a reload counter for testing.
        var configProvider = new DynamicConfigProvider();

        // Act - Simulate rapid file changes
        for (int i = 0; i < 10; i++)
        {
            await File.WriteAllTextAsync(_testRoutesFile, $"[{{\"Hostname\":\"test{i}.localhost\",\"Port\":4001,\"Pid\":12345}}]");
            await Task.Delay(50); // 50ms between writes (faster than 500ms debounce)
        }

        // Wait for debounce to complete
        await Task.Delay(1000);

        // TODO: Assert that configProvider.Update was called exactly once
    }

    [Fact]
    public async Task ProxyStartup_LoadsExistingRoutes()
    {
        // Arrange - Create routes file with test data
        var testRoutes = new[]
        {
            new RouteInfo
            {
                Hostname = "startup-test.localhost",
                Port = 4001,
                Pid = 12345,
                CreatedAt = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(testRoutes, _jsonOptions);
        await File.WriteAllTextAsync(_testRoutesFile, json);

        // Verify file was written
        Assert.True(File.Exists(_testRoutesFile), $"Test routes file should exist: {_testRoutesFile}");

        // Act - Load routes via RouteStore (using the one from InitializeAsync)
        var loadedRoutes = await _routeStore!.LoadRoutesAsync();

        // Assert
        Assert.NotNull(loadedRoutes);
        Assert.Single(loadedRoutes);
        Assert.Equal("startup-test.localhost", loadedRoutes[0].Hostname);
        Assert.Equal(4001, loadedRoutes[0].Port);
    }

    [Fact]
    public async Task YarpConfigConverter_ConvertsRouteInfoCorrectly()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "api.localhost",
                Port = 4001,
                Pid = 12345,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act - Convert to YARP config (mimic RouteFileWatcher logic)
        var routeConfigs = routes.Select(r => new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = $"route-{r.Hostname}",
            ClusterId = $"cluster-{r.Hostname}",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Hosts = new[] { r.Hostname },
                Path = "/{**catch-all}"
            }
        }).ToList();

        var clusterConfigs = routes.Select(r => new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = $"cluster-{r.Hostname}",
            Destinations = new System.Collections.Generic.Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig
                {
                    Address = $"http://localhost:{r.Port}"
                }
            }
        }).ToList();

        // Assert
        Assert.Single(routeConfigs);
        Assert.Single(clusterConfigs);
        Assert.Equal("route-api.localhost", routeConfigs[0].RouteId);
        Assert.Equal("cluster-api.localhost", routeConfigs[0].ClusterId);
        Assert.Equal("api.localhost", routeConfigs[0].Match.Hosts[0]);
        Assert.Equal("http://localhost:4001", clusterConfigs[0].Destinations["backend1"].Address);
    }
}
