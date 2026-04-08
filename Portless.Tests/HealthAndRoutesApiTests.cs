using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Integration tests for the /health, /api/v1/routes, and /api/v1/status endpoints.
/// </summary>
[Collection("Integration Tests")]
public class HealthAndRoutesApiTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _tempDir = null!;

    public async Task InitializeAsync()
    {
        // Use isolated temp state directory to prevent interference from other tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-health-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _tempDir);
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "routes.json"), "[]");

        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
        // Cleanup only our own temp dir
        try
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task RoutesEndpoint_ReturnsArray()
    {
        var response = await _client.GetAsync("/api/v1/routes");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("[", content.Trim());
    }

    [Fact]
    public async Task StatusEndpoint_ReturnsRunning()
    {
        var response = await _client.GetAsync("/api/v1/status");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("running", content);
    }
}
