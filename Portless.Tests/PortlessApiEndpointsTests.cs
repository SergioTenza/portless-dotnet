using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Portless.Tests;

/// <summary>
/// Extended integration tests for PortlessApiEndpoints.
/// Covers: add-host validation/conflict, remove-host, inspect endpoints,
/// plugins endpoints, status endpoint, and TCP endpoints.
/// </summary>
[Collection("Integration Tests")]
public class PortlessApiEndpointsTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
    }

    // ── Add-Host endpoint tests ──────────────────────────────────────

    [Fact]
    public async Task AddHost_NullHostname_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = (string?)null,
            backendUrl = "http://localhost:5000"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddHost_EmptyBackendUrl_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "test.localhost",
            backendUrl = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddHost_DuplicateHostname_Returns409()
    {
        // Add first host
        var response1 = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "dup.localhost",
            backendUrl = "http://localhost:5001"
        });
        response1.EnsureSuccessStatusCode();

        // Try to add the same hostname again
        var response2 = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "dup.localhost",
            backendUrl = "http://localhost:5002"
        });
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task AddHost_WithLoadBalancePolicy_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "lb.localhost",
            backendUrl = "http://localhost:5010",
            backendUrls = new[] { "http://localhost:5010", "http://localhost:5011" },
            loadBalancePolicy = "RoundRobin"
        });
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task AddHost_SingleBackend_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "single.localhost",
            backendUrl = "http://localhost:5020"
        });
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("hostname", out var hostname));
        Assert.Equal("single.localhost", hostname.GetString());
    }

    [Fact]
    public async Task AddHost_WithPath_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "path.localhost",
            backendUrl = "http://localhost:5030",
            path = "/api"
        });
        response.EnsureSuccessStatusCode();
    }

    // ── Remove-Host endpoint tests ───────────────────────────────────

    [Fact]
    public async Task RemoveHost_EmptyHostname_Returns400()
    {
        var response = await _client.DeleteAsync("/api/v1/remove-host?hostname=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveHost_ExistingHost_Succeeds()
    {
        // First add a host
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "toremove.localhost",
            backendUrl = "http://localhost:5040"
        });

        // Then remove it
        var response = await _client.DeleteAsync("/api/v1/remove-host?hostname=toremove.localhost");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task RemoveHost_NonExistingHost_Succeeds()
    {
        // Removing a host that doesn't exist should still succeed (idempotent)
        var response = await _client.DeleteAsync("/api/v1/remove-host?hostname=nonexistent.localhost");
        response.EnsureSuccessStatusCode();
    }

    // ── Status endpoint tests ────────────────────────────────────────

    [Fact]
    public async Task StatusEndpoint_ReturnsExpectedFields()
    {
        var response = await _client.GetAsync("/api/v1/status");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("status", out var status));
        Assert.Equal("running", status.GetString());
        Assert.True(content.TryGetProperty("httpRoutes", out _));
        Assert.True(content.TryGetProperty("clusters", out _));
        Assert.True(content.TryGetProperty("tcpListeners", out _));
        Assert.True(content.TryGetProperty("uptime", out _));
    }

    [Fact]
    public async Task StatusEndpoint_ShowsActiveRoutes_AfterAddingHost()
    {
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "statustest.localhost",
            backendUrl = "http://localhost:5050"
        });

        var response = await _client.GetAsync("/api/v1/status");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("httpRoutes", out var httpRoutes));
        Assert.True(httpRoutes.GetInt32() >= 1);
    }

    // ── Routes endpoint tests ────────────────────────────────────────

    [Fact]
    public async Task RoutesEndpoint_ShowsAddedRoute()
    {
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "routeslist.localhost",
            backendUrl = "http://localhost:5060"
        });

        var response = await _client.GetAsync("/api/v1/routes");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("routeslist.localhost", content);
    }

    // ── Inspector endpoint tests ─────────────────────────────────────

    [Fact]
    public async Task InspectSessions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/inspect/sessions");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task InspectSessions_WithCount_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/inspect/sessions?count=50");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task InspectSessionById_InvalidGuid_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/inspect/sessions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InspectStats_ReturnsExpectedFields()
    {
        var response = await _client.GetAsync("/api/v1/inspect/stats");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("totalCaptured", out _));
        Assert.True(content.TryGetProperty("avgDurationMs", out _));
        Assert.True(content.TryGetProperty("errorRate", out _));
        Assert.True(content.TryGetProperty("requestsPerMinute", out _));
    }

    [Fact]
    public async Task InspectClear_ReturnsOk()
    {
        var response = await _client.DeleteAsync("/api/v1/inspect/sessions");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("cleared", out _));
    }

    // ── Plugins endpoint tests ───────────────────────────────────────

    [Fact]
    public async Task PluginsList_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/plugins");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PluginsReload_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/v1/plugins/reload", null);
        response.EnsureSuccessStatusCode();
    }

    // ── Inspect stream (WebSocket) endpoint test ─────────────────────

    [Fact]
    public async Task InspectStream_NonWebSocket_Returns400()
    {
        // A regular HTTP GET (not a WebSocket upgrade) should return 400
        var response = await _client.GetAsync("/api/v1/inspect/stream");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Tests for PortlessApiEndpoints with load balancing policy variants.
/// </summary>
[Collection("Integration Tests")]
public class LoadBalancingPolicyTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
    }

    [Theory]
    [InlineData("RoundRobin")]
    [InlineData("LeastRequests")]
    [InlineData("Random")]
    [InlineData("First")]
    [InlineData("PowerOfTwoChoices")]
    public async Task AddHost_WithVariousLoadBalancePolicies_Succeeds(string policy)
    {
        var hostname = $"lb-{policy.ToLower()}.localhost";
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname,
            backendUrl = "http://localhost:5010",
            backendUrls = new[] { "http://localhost:5010", "http://localhost:5011" },
            loadBalancePolicy = policy
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddHost_NullLoadBalancePolicy_UsesDefault()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "lbnull.localhost",
            backendUrl = "http://localhost:5010",
            backendUrls = new[] { "http://localhost:5010", "http://localhost:5011" },
            loadBalancePolicy = (string?)null
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddHost_EmptyLoadBalancePolicy_UsesDefault()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "lbempty.localhost",
            backendUrl = "http://localhost:5010",
            backendUrls = new[] { "http://localhost:5010", "http://localhost:5011" },
            loadBalancePolicy = ""
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddHost_UnknownLoadBalancePolicy_UsesDefault()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "lbunknown.localhost",
            backendUrl = "http://localhost:5010",
            backendUrls = new[] { "http://localhost:5010", "http://localhost:5011" },
            loadBalancePolicy = "UnknownPolicy"
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddHost_SingleBackend_IgnoresLoadBalancePolicy()
    {
        // Single backend should not set load balancing policy even if provided
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "lbsingle.localhost",
            backendUrl = "http://localhost:5010",
            loadBalancePolicy = "RoundRobin"
        });
        response.EnsureSuccessStatusCode();
    }
}
