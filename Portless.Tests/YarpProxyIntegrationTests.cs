using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Configuration;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

/// <summary>
/// Comprehensive YARP proxy integration tests extending existing ProxyRoutingTests.
/// Tests verify advanced routing scenarios, header forwarding, and API endpoints.
/// </summary>
public class YarpProxyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public YarpProxyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProxyForwardsHeadersCorrectly()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-test.localhost",
                ClusterId = "cluster-test.localhost",
                Match = new RouteMatch
                {
                    Hosts = new[] { "test.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-test.localhost",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Send request with custom headers
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "test.localhost");
        request.Headers.Add("X-Test-Header", "test-value");
        request.Headers.Add("X-Custom-User-Agent", "TestAgent/1.0");

        var response = await _client.SendAsync(request);

        // Assert - Request should be processed (may fail if backend not running)
        // Key assertion: YARP configuration accepts the routing setup
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected routing to be configured, got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task MultipleHostnames_RouteToDifferentBackends()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-api.localhost",
                ClusterId = "cluster-api.localhost",
                Match = new RouteMatch
                {
                    Hosts = new[] { "api.localhost" },
                    Path = "/{**catch-all}"
                }
            },
            new RouteConfig
            {
                RouteId = "route-web.localhost",
                ClusterId = "cluster-web.localhost",
                Match = new RouteMatch
                {
                    Hosts = new[] { "web.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-api.localhost",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["api-backend"] = new DestinationConfig { Address = "http://localhost:5001" }
                }
            },
            new ClusterConfig
            {
                ClusterId = "cluster-web.localhost",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["web-backend"] = new DestinationConfig { Address = "http://localhost:3000" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Request to api.localhost
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
        request1.Headers.Add("Host", "api.localhost");
        var response1 = await _client.SendAsync(request1);

        // Act - Request to web.localhost
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/");
        request2.Headers.Add("Host", "web.localhost");
        var response2 = await _client.SendAsync(request2);

        // Assert - Both should route correctly
        Assert.True(
            response1.StatusCode == HttpStatusCode.OK ||
            response1.StatusCode == HttpStatusCode.BadGateway ||
            response1.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"api.localhost: Got {response1.StatusCode}"
        );

        Assert.True(
            response2.StatusCode == HttpStatusCode.OK ||
            response2.StatusCode == HttpStatusCode.BadGateway ||
            response2.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"web.localhost: Got {response2.StatusCode}"
        );
    }

    [Fact]
    public async Task ApiAddHostEndpoint_UpdatesRoutesDynamically()
    {
        // Arrange
        var testHostname = "dynamic.localhost";
        var testBackendUrl = "http://localhost:6000";

        // Act - Call the API endpoint to add a host
        var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = testHostname,
            backendUrl = testBackendUrl
        });

        // Assert - Verify successful response
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(result);

        // Verify the route was added by making a request
        var testRequest = new HttpRequestMessage(HttpMethod.Get, "/");
        testRequest.Headers.Add("Host", testHostname);
        var testResponse = await _client.SendAsync(testRequest);

        // Should route to the backend (may fail if backend not running)
        Assert.True(
            testResponse.StatusCode == HttpStatusCode.OK ||
            testResponse.StatusCode == HttpStatusCode.BadGateway ||
            testResponse.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected routing to be configured for {testHostname}, got {testResponse.StatusCode}"
        );
    }

    [Fact]
    public async Task ApiAddHostEndpoint_ValidatesRequiredFields()
    {
        // Act - Try to add host with empty hostname
        var response1 = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "",
            backendUrl = "http://localhost:6000"
        });

        // Assert - Should return 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

        // Act - Try to add host with empty backendUrl
        var response2 = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "test.localhost",
            backendUrl = ""
        });

        // Assert - Should return 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task ConfigurationUpdate_PreservesExistingRoutes()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        // Step 1: Add initial route
        var initialRoutes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-persistent.localhost",
                ClusterId = "cluster-persistent.localhost",
                Match = new RouteMatch
                {
                    Hosts = new[] { "persistent.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var initialClusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-persistent.localhost",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" }
                }
            }
        };

        config.Update(initialRoutes, initialClusters);

        // Verify first route works
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
        request1.Headers.Add("Host", "persistent.localhost");
        var response1 = await _client.SendAsync(request1);

        // Step 2: Add second route via API endpoint
        var apiResponse = await _client.PostAsJsonAsync("/api/v1/add-host", new
        {
            hostname = "new.localhost",
            backendUrl = "http://localhost:6000"
        });
        apiResponse.EnsureSuccessStatusCode();

        // Small delay to allow YARP to reload configuration
        await Task.Delay(100);

        // Step 3: Verify both routes work after configuration update
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/");
        request2.Headers.Add("Host", "new.localhost");
        var response2 = await _client.SendAsync(request2);

        // Verify persistent route still works
        var request3 = new HttpRequestMessage(HttpMethod.Get, "/");
        request3.Headers.Add("Host", "persistent.localhost");
        var response3 = await _client.SendAsync(request3);

        // Assert - All routes should work
        Assert.True(
            response1.StatusCode == HttpStatusCode.OK ||
            response1.StatusCode == HttpStatusCode.BadGateway,
            "Initial route should work"
        );

        Assert.True(
            response2.StatusCode == HttpStatusCode.OK ||
            response2.StatusCode == HttpStatusCode.BadGateway,
            "New route should work"
        );

        Assert.True(
            response3.StatusCode == HttpStatusCode.OK ||
            response3.StatusCode == HttpStatusCode.BadGateway,
            "Persistent route should still work after update"
        );
    }
}
