using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portless.Core.Services;

namespace Portless.Tests.Dashboard;

[Collection("Integration Tests")]
public sealed class DashboardApiIntegrationTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    protected override string TempDirPrefix => "portless-test-dashboard";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
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
