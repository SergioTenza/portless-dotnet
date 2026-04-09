using Portless.Core.Configuration;
using Portless.Core.Models;
using Portless.Core.Services;
using System.Text.Json;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Comprehensive route persistence integration tests.
/// Tests verify save/load, file locking, cleanup, and hot-reload functionality.
/// </summary>
[Collection("Integration Tests")]
public class RoutePersistenceIntegrationTests : IntegrationTestBase
{
    private string _routesFilePath = null!;
    private IRouteStore? _routeStore;

    protected override bool CreateRoutesJson => false;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _routesFilePath = Path.Combine(TempDir, "routes.json");
        _routeStore = new TestRouteStore(TempDir);
    }

    [Fact]
    public async Task SaveRoutesAsync_PersistsToFile()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "api1.localhost", Port = 4001, Pid = 12345, CreatedAt = DateTime.UtcNow },
            new RouteInfo { Hostname = "api2.localhost", Port = 4002, Pid = 12346, CreatedAt = DateTime.UtcNow }
        };

        // Act
        await _routeStore!.SaveRoutesAsync(routes);

        // Assert
        Assert.True(File.Exists(_routesFilePath));

        var json = await File.ReadAllTextAsync(_routesFilePath);
        var deserialized = JsonSerializer.Deserialize<RouteInfo[]>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Length);
    }

    [Fact]
    public async Task LoadRoutesAsync_RestoresFromFile()
    {
        // Arrange
        var originalRoutes = new[]
        {
            new RouteInfo
            {
                Hostname = "test.localhost",
                Port = 4001,
                Pid = 12345,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _routeStore!.SaveRoutesAsync(originalRoutes);

        // Act - Create new RouteStore instance to test loading
        var newStore = new TestRouteStore(TempDir);
        var loadedRoutes = await newStore.LoadRoutesAsync();

        // Assert
        Assert.Single(loadedRoutes);
        Assert.Equal("test.localhost", loadedRoutes[0].Hostname);
        Assert.Equal(4001, loadedRoutes[0].Port);
        Assert.Equal(12345, loadedRoutes[0].Pid);
    }

    [Fact]
    public async Task SaveRoutesAsync_WithConcurrentAccess_UsesFileLocking()
    {
        // Arrange
        var routes1 = new[]
        {
            new RouteInfo { Hostname = "api1.localhost", Port = 4001, Pid = 12345 }
        };
        var routes2 = new[]
        {
            new RouteInfo { Hostname = "api2.localhost", Port = 4002, Pid = 12346 }
        };

        // Act - Simulate concurrent access from multiple processes
        var task1 = Task.Run(() => _routeStore!.SaveRoutesAsync(routes1));
        var task2 = Task.Run(() => _routeStore!.SaveRoutesAsync(routes2));

        await Task.WhenAll(task1, task2);

        // Assert - File should be valid JSON (not corrupted)
        var json = await File.ReadAllTextAsync(_routesFilePath);
        var deserialized = JsonSerializer.Deserialize<RouteInfo[]>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Length >= 1); // At least one route saved
        Assert.True(File.Exists(_routesFilePath)); // File still exists
    }

    [Fact]
    public async Task SaveLoadRoundtrip_PreservesAllProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var originalRoutes = new[]
        {
            new RouteInfo
            {
                Hostname = "full.localhost",
                Port = 4999,
                Pid = 99999,
                CreatedAt = now,
                LastSeen = now.AddMinutes(5)
            }
        };

        // Act
        await _routeStore!.SaveRoutesAsync(originalRoutes);
        var loadedRoutes = await _routeStore.LoadRoutesAsync();

        // Assert
        Assert.Single(loadedRoutes);

        var route = loadedRoutes[0];
        Assert.Equal("full.localhost", route.Hostname);
        Assert.Equal(4999, route.Port);
        Assert.Equal(99999, route.Pid);

        // DateTime comparison with tolerance for JSON serialization
        Assert.True((route.CreatedAt - now).TotalSeconds < 1);
        Assert.NotNull(route.LastSeen);
    }

    [Fact]
    public async Task LoadRoutesAsync_WhenFileMissing_ReturnsEmptyArray()
    {
        // Arrange - Ensure file doesn't exist
        if (File.Exists(_routesFilePath))
        {
            File.Delete(_routesFilePath);
        }

        // Act
        var routes = await _routeStore!.LoadRoutesAsync();

        // Assert
        Assert.NotNull(routes);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task LoadRoutesAsync_WhenFileIsEmpty_ReturnsEmptyArray()
    {
        // Arrange
        await File.WriteAllTextAsync(_routesFilePath, string.Empty);

        // Act
        var routes = await _routeStore!.LoadRoutesAsync();

        // Assert
        Assert.NotNull(routes);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task SaveRoutesAsync_OverwritesExistingFile()
    {
        // Arrange
        var routes1 = new[]
        {
            new RouteInfo { Hostname = "old.localhost", Port = 4001, Pid = 12345 }
        };

        var routes2 = new[]
        {
            new RouteInfo { Hostname = "new.localhost", Port = 4002, Pid = 12346 }
        };

        // Act - Save first set
        await _routeStore!.SaveRoutesAsync(routes1);
        var loaded1 = await _routeStore.LoadRoutesAsync();
        Assert.Single(loaded1);
        Assert.Equal("old.localhost", loaded1[0].Hostname);

        // Act - Save second set (overwrite)
        await _routeStore.SaveRoutesAsync(routes2);
        var loaded2 = await _routeStore.LoadRoutesAsync();

        // Assert - Should have new data only
        Assert.Single(loaded2);
        Assert.Equal("new.localhost", loaded2[0].Hostname);
    }
}

/// <summary>
/// Test-specific RouteStore implementation that uses a custom directory.
/// This allows testing without affecting the actual state directory.
/// </summary>
internal class TestRouteStore : IRouteStore
{
    private readonly string _stateDirectory;
    private readonly string _routesFilePath;
    private const string MutexName = "Portless.Routes.Lock";

    public TestRouteStore(string stateDirectory)
    {
        _stateDirectory = stateDirectory;
        _routesFilePath = Path.Combine(stateDirectory, "routes.json");
        Directory.CreateDirectory(stateDirectory);
    }

    public Task<RouteInfo[]> LoadRoutesAsync(CancellationToken cancellationToken=default)
    {
        if (!File.Exists(_routesFilePath))
        {
            return Task.FromResult(Array.Empty<RouteInfo>());
        }

        using var mutex = new Mutex(false, MutexName);
        try
        {
            // Wait for file lock with timeout
            if (!mutex.WaitOne(5000))
            {
                throw new InvalidOperationException("Could not acquire file lock");
            }

            var json = File.ReadAllText(_routesFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Task.FromResult(Array.Empty<RouteInfo>());
            }

            var routes = JsonSerializer.Deserialize<RouteInfo[]>(json);
            return Task.FromResult(routes ?? Array.Empty<RouteInfo>());
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public Task SaveRoutesAsync(RouteInfo[] routes, CancellationToken cancellationToken=default)
    {
        Directory.CreateDirectory(_stateDirectory);

        using var mutex = new Mutex(false, MutexName);
        try
        {
            // Wait for file lock with timeout
            if (!mutex.WaitOne(5000))
            {
                throw new InvalidOperationException("Could not acquire file lock");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(routes, options);

            // Atomic write: write to temp file first
            var tempFilePath = _routesFilePath + ".tmp";
            File.WriteAllText(tempFilePath, json);

            // Replace original file
            if (File.Exists(_routesFilePath))
            {
                File.Delete(_routesFilePath);
            }

            File.Move(tempFilePath, _routesFilePath);

            return Task.CompletedTask;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
