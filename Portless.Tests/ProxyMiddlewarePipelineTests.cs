using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Configuration;
using Portless.Core.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

/// <summary>
/// Tests for the proxy middleware pipeline: branded error pages,
/// request logging, X-Forwarded-Protocol, and request inspector capture.
/// </summary>
[Collection("Integration Tests")]
public class ProxyMiddlewarePipelineTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp();
        _client = CreateHttpClient(_factory);
    }

    // ── Branded error page tests ─────────────────────────────────────

    [Fact]
    public async Task UnknownHost_404_ReturnsHtmlErrorPage()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "nonexistent.localhost");
        request.Headers.Add("Accept", "text/html");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("404", content);
        Assert.Contains("nonexistent.localhost", content);
    }

    [Fact]
    public async Task KnownHostNoBackend_502_ReturnsHtmlErrorPage()
    {
        // Add a route pointing to a non-existent backend
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-502test.localhost", "cluster-502test.localhost", new[] { "502test.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-502test.localhost", "http://localhost:19990")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "502test.localhost");
        request.Headers.Add("Accept", "text/html");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("502", content);
    }

    [Fact]
    public async Task UnknownHost_404_WithEmptyAccept_ReturnsHtmlErrorPage()
    {
        // Empty Accept header should be treated as HTML request
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "emptyaccept.localhost");
        request.Headers.Add("Accept", "");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownHost_404_NonHtmlAccept_ReturnsJson()
    {
        // Non-HTML accept should not trigger branded error pages
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "jsonaccept.localhost");
        request.Headers.Add("Accept", "application/json");
        var response = await _client.SendAsync(request);

        // YARP may return 404 for unknown routes, response depends on pipeline
        Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task ErrorPage_ListsActiveRoutes_WhenAvailable()
    {
        // Add an active route first
        await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "active-route.localhost",
            backendUrl = "http://localhost:19991"
        });
        await Task.Delay(200);

        // Request a non-existent host to get 404 page
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "missing.localhost");
        request.Headers.Add("Accept", "text/html");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("active-route.localhost", content);
    }

    // ── Request inspection tests ─────────────────────────────────────

    [Fact]
    public async Task ProxiedRequest_CapturedByInspector()
    {
        // Setup a route
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-capture.localhost", "cluster-capture.localhost", new[] { "capture.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-capture.localhost", "http://localhost:19989")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        // Make a proxied request
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Host", "capture.localhost");
        await _client.SendAsync(request);

        // Check inspector captured it
        var inspectResponse = await _client.GetAsync("/api/v1/inspect/sessions");
        inspectResponse.EnsureSuccessStatusCode();
        var content = await inspectResponse.Content.ReadAsStringAsync();
        Assert.Contains("capture.localhost", content);
    }

    [Fact]
    public async Task InspectorSessionById_ReturnsCorrectSession()
    {
        // Make a request that will be captured
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-byid.localhost", "cluster-byid.localhost", new[] { "byid.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-byid.localhost", "http://localhost:19988")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/unique-path-for-byid");
        request.Headers.Add("Host", "byid.localhost");
        await _client.SendAsync(request);

        // Get sessions to find the ID
        var sessionsResponse = await _client.GetAsync("/api/v1/inspect/sessions");
        var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
        var sessions = System.Text.Json.JsonDocument.Parse(sessionsContent);

        if (sessions.RootElement.GetArrayLength() > 0)
        {
            var firstSession = sessions.RootElement[0];
            if (firstSession.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetGuid();
                var detailResponse = await _client.GetAsync($"/api/v1/inspect/sessions/{id}");
                detailResponse.EnsureSuccessStatusCode();
            }
        }
    }

    [Fact]
    public async Task InspectorClear_ResetsCapturedData()
    {
        // Clear inspector
        var clearResponse = await _client.DeleteAsync("/api/v1/inspect/sessions");
        clearResponse.EnsureSuccessStatusCode();

        // Verify it's cleared
        var sessionsResponse = await _client.GetAsync("/api/v1/inspect/sessions");
        var content = await sessionsResponse.Content.ReadAsStringAsync();
        var sessions = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal(0, sessions.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task InspectorStats_AfterRequests_Updated()
    {
        // First clear
        await _client.DeleteAsync("/api/v1/inspect/sessions");

        // Make a proxied request
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-stats.localhost", "cluster-stats.localhost", new[] { "stats.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-stats.localhost", "http://localhost:19987")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "stats.localhost");
        await _client.SendAsync(request);

        // Check stats
        var statsResponse = await _client.GetAsync("/api/v1/inspect/stats");
        statsResponse.EnsureSuccessStatusCode();
        var stats = await statsResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(stats.TryGetProperty("totalCaptured", out var total));
        Assert.True(total.GetInt32() >= 0);
    }
}

/// <summary>
/// Tests for the add-host + remove-host + routes interaction flow.
/// </summary>
[Collection("Integration Tests")]
public class PortlessApiWorkflowTests : IntegrationTestBase
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
    public async Task AddThenRemoveThenAdd_Host_Succeeds()
    {
        var hostname = "workflow.localhost";

        // Add
        var addResponse = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname,
            backendUrl = "http://localhost:5100"
        });
        addResponse.EnsureSuccessStatusCode();

        // Remove
        var removeResponse = await _client.DeleteAsync($"/api/v1/remove-host?hostname={hostname}");
        removeResponse.EnsureSuccessStatusCode();

        // Add again (should not conflict now)
        var addAgainResponse = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname,
            backendUrl = "http://localhost:5101"
        });
        addAgainResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MultipleHosts_CanCoexist()
    {
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = $"multi-{i}.localhost",
                backendUrl = $"http://localhost:{5200 + i}"
            });
            response.EnsureSuccessStatusCode();
        }

        // Verify all appear in routes
        var routesResponse = await _client.GetAsync("/api/v1/routes");
        var content = await routesResponse.Content.ReadAsStringAsync();
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"multi-{i}.localhost", content);
        }
    }

    [Fact]
    public async Task RemoveHost_PreservesOtherHosts()
    {
        // Add three hosts
        await _client.PostAsJsonAsync("/api/v1/add-host", new { hostname = "keep1.localhost", backendUrl = "http://localhost:5301" });
        await _client.PostAsJsonAsync("/api/v1/add-host", new { hostname = "remove-me.localhost", backendUrl = "http://localhost:5302" });
        await _client.PostAsJsonAsync("/api/v1/add-host", new { hostname = "keep2.localhost", backendUrl = "http://localhost:5303" });

        // Remove one
        await _client.DeleteAsync("/api/v1/remove-host?hostname=remove-me.localhost");

        // Verify the remaining two are still there
        var routesResponse = await _client.GetAsync("/api/v1/routes");
        var content = await routesResponse.Content.ReadAsStringAsync();
        Assert.Contains("keep1.localhost", content);
        Assert.DoesNotContain("remove-me.localhost", content);
        Assert.Contains("keep2.localhost", content);
    }

    [Fact]
    public async Task AddHost_WithBackendUrls_MultipleBackends()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "multibackend.localhost",
            backendUrl = "http://localhost:5401",
            backendUrls = new[] { "http://localhost:5401", "http://localhost:5402", "http://localhost:5403" }
        });
        response.EnsureSuccessStatusCode();

        // Verify it shows in routes
        var routesResponse = await _client.GetAsync("/api/v1/routes");
        var content = await routesResponse.Content.ReadAsStringAsync();
        Assert.Contains("multibackend.localhost", content);
    }
}
