using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portless.Core.Services;

namespace Portless.Tests.Dashboard;

[Collection("Integration Tests")]
public sealed class DashboardApiIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _tempDir;

    public DashboardApiIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-dashboard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PORTLESS_STATE_DIR", _tempDir);
        });
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task DashboardSummary_Returns200WithExpectedShape()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/dashboard/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("activeRoutes", out _));
        Assert.True(json.TryGetProperty("uptime", out _));
        Assert.True(json.TryGetProperty("totalCaptured", out _));
        Assert.True(json.TryGetProperty("avgDurationMs", out _));
        Assert.True(json.TryGetProperty("errorRate", out _));
        Assert.True(json.TryGetProperty("requestsPerMinute", out _));
    }

    [Fact]
    public async Task DashboardRoutes_Returns200WithArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/dashboard/routes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task DashboardEvents_ReturnsSSEStream()
    {
        // Act - read just the beginning of the SSE stream
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var response = await _client.GetAsync("/api/v1/dashboard/events", HttpCompletionOption.ResponseHeadersRead, cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }
}
