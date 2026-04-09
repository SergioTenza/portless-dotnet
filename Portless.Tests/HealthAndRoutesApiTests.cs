using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Integration tests for the /health, /api/v1/routes, and /api/v1/status endpoints.
/// </summary>
[Collection("Integration Tests")]
public class HealthAndRoutesApiTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
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
