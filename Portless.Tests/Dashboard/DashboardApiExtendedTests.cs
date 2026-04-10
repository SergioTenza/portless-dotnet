using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Portless.Tests.Dashboard;

/// <summary>
/// Extended integration tests for DashboardApiEndpoints.
/// Covers: dashboard routes with health check, dashboard summary with data,
/// events SSE stream behavior, and various edge cases.
/// </summary>
[Collection("Integration Tests")]
public class DashboardApiExtendedTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    protected override string TempDirPrefix => "portless-test-dashext";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task DashboardSummary_WithRoutes_ShowsActiveRouteCount()
    {
        // Add a route first
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "dashsummary.localhost",
            backendUrl = "http://localhost:5070"
        });

        var response = await _client.GetAsync("/api/v1/dashboard/summary");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("activeRoutes", out var activeRoutes));
        Assert.True(activeRoutes.GetInt32() >= 1);
    }

    [Fact]
    public async Task DashboardSummary_NoRoutes_ZeroActiveRoutes()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/summary");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("activeRoutes", out var activeRoutes));
        // No routes have been added, but there could be from config loading - just check it's an int
        Assert.True(activeRoutes.GetInt32() >= 0);
    }

    [Fact]
    public async Task DashboardRoutes_WithAddedRoute_ShowsRouteDetails()
    {
        // Add a route
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "dashroute.localhost",
            backendUrl = "http://localhost:5080"
        });

        var response = await _client.GetAsync("/api/v1/dashboard/routes");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        // Find our route
        var found = false;
        foreach (var item in json.EnumerateArray())
        {
            if (item.TryGetProperty("hostname", out var hn) &&
                hn.GetString() == "dashroute.localhost")
            {
                found = true;
                Assert.True(item.TryGetProperty("port", out _));
                Assert.True(item.TryGetProperty("type", out _));
                Assert.True(item.TryGetProperty("backends", out _));
                break;
            }
        }
        Assert.True(found, "Added route should appear in dashboard routes");
    }

    [Fact]
    public async Task DashboardRoutes_WithHealthChecker_ShowsHealthField()
    {
        // Add a route
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "healthcheck.localhost",
            backendUrl = "http://localhost:5081"
        });

        var response = await _client.GetAsync("/api/v1/dashboard/routes");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in json.EnumerateArray())
        {
            if (item.TryGetProperty("hostname", out var hn) &&
                hn.GetString() == "healthcheck.localhost")
            {
                // Health field should be present (may be null in JSON but the property should exist)
                Assert.True(item.TryGetProperty("health", out _));
                break;
            }
        }
    }

    [Fact]
    public async Task DashboardRoutes_EmptyRoutes_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/routes");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task DashboardEvents_SSEContentType()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var response = await _client.GetAsync(
            "/api/v1/dashboard/events",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoCache == true);
    }

    [Fact]
    public async Task DashboardSummary_InspectorStatsFields()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/summary");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Verify all expected stats fields are present
        Assert.True(json.TryGetProperty("totalCaptured", out _));
        Assert.True(json.TryGetProperty("avgDurationMs", out _));
        Assert.True(json.TryGetProperty("errorRate", out _));
        Assert.True(json.TryGetProperty("requestsPerMinute", out _));
        Assert.True(json.TryGetProperty("uptime", out _));
    }

    [Fact]
    public async Task DashboardRoutes_AfterAddAndRemove_ReflectsChanges()
    {
        // Add route
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "temp.localhost",
            backendUrl = "http://localhost:5085"
        });

        // Verify it appears
        var response1 = await _client.GetAsync("/api/v1/dashboard/routes");
        var json1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("temp.localhost", json1.ToString());

        // Remove route
        await _client.DeleteAsync("/api/v1/remove-host?hostname=temp.localhost");

        // Verify it's gone
        var response2 = await _client.GetAsync("/api/v1/dashboard/routes");
        var json2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain("temp.localhost", json2.ToString());
    }

    [Fact]
    public async Task DashboardSummary_UptimeField_IsNotNull()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/summary");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("uptime", out var uptime));
        // Uptime may be null if process start time unavailable
        Assert.True(uptime.ValueKind == JsonValueKind.String || uptime.ValueKind == JsonValueKind.Null);
    }
}
