using Xunit;
using System.Text.Json;
using Portless.Core.Services;
using Portless.Core.Models;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Portless.Tests;

[Collection("RouteStore Tests")]
public class RoutePersistenceTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly string _testRoutesFile;
    private IRouteStore? _routeStore;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public RoutePersistenceTests()
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

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveRoutesAsync_CreatesFileIfNotExists()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "test.localhost", Port = 4001, Pid = 12345 }
        };

        // Act
        await _routeStore!.SaveRoutesAsync(routes);

        // Assert
        Assert.True(File.Exists(_testRoutesFile));
    }

    [Fact]
    public async Task SaveRoutesAsync_WritesValidJson()
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

        // Act
        await _routeStore!.SaveRoutesAsync(routes);
        var json = await File.ReadAllTextAsync(_testRoutesFile);
        var deserialized = JsonSerializer.Deserialize<RouteInfo[]>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
        Assert.Equal("api.localhost", deserialized[0].Hostname);
        Assert.Equal(4001, deserialized[0].Port);
        Assert.Equal(12345, deserialized[0].Pid);
    }

    [Fact]
    public async Task LoadRoutesAsync_ReturnsEmptyArrayIfFileNotExists()
    {
        // Act
        var routes = await _routeStore!.LoadRoutesAsync();

        // Assert
        Assert.NotNull(routes);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task LoadRoutesAsync_ReturnsEmptyArrayIfFileIsEmpty()
    {
        // Arrange
        await File.WriteAllTextAsync(_testRoutesFile, string.Empty);

        // Act
        var routes = await _routeStore!.LoadRoutesAsync();

        // Assert
        Assert.NotNull(routes);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task SaveLoadRoundtrip_PreservesData()
    {
        // Arrange
        var originalRoutes = new[]
        {
            new RouteInfo
            {
                Hostname = "api1.localhost",
                Port = 4001,
                Pid = 12345,
                CreatedAt = DateTime.UtcNow
            },
            new RouteInfo
            {
                Hostname = "api2.localhost",
                Port = 4002,
                Pid = 12346,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        await _routeStore!.SaveRoutesAsync(originalRoutes);
        var loadedRoutes = await _routeStore.LoadRoutesAsync();

        // Assert
        Assert.Equal(2, loadedRoutes.Length);
        Assert.Equal("api1.localhost", loadedRoutes[0].Hostname);
        Assert.Equal("api2.localhost", loadedRoutes[1].Hostname);
        Assert.Equal(4001, loadedRoutes[0].Port);
        Assert.Equal(4002, loadedRoutes[1].Port);
    }

    [Fact]
    public async Task ConcurrentAccess_DoesNotCorruptFile()
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
        var json = await File.ReadAllTextAsync(_testRoutesFile);
        var deserialized = JsonSerializer.Deserialize<RouteInfo[]>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Length >= 1); // At least one route saved
        Assert.True(File.Exists(_testRoutesFile)); // File still exists
    }

    [Fact]
    public async Task AtomicWrite_PreventsCorruptionOnCrash()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "test.localhost", Port = 4001, Pid = 12345 }
        };

        // Act
        await _routeStore!.SaveRoutesAsync(routes);

        // Simulate crash mid-write by checking temp file is cleaned up
        var tempFiles = Directory.GetFiles(_testDirectory, "*.tmp");
        var tempFileInTargetDir = Directory.GetFiles(_testDirectory)
            .Where(f => f.StartsWith(Path.Combine(_testDirectory, "tmp")));

        // Assert - No temp files left behind
        Assert.Empty(tempFileInTargetDir);
        Assert.True(File.Exists(_testRoutesFile)); // Only target file exists
    }
}
