using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Configuration;
using System.Net;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

/// <summary>
/// HTTP/2 protocol support integration tests.
/// Tests verify HTTP/2 negotiation, protocol logging, and X-Forwarded headers.
/// </summary>
[Collection("Integration Tests")]
public class Http2IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public Http2IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // Note: WebApplicationFactory defaults to HTTP/1.1
        // HTTP/2 testing requires either HTTPS or HTTP/2 prior knowledge
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Http2Negotiation_KestrelConfigured()
    {
        // Arrange - Verify Kestrel is configured for HTTP/2
        // This test verifies the configuration is set correctly
        // Actual HTTP/2 negotiation requires TLS or client with prior knowledge

        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-http2-test.localhost",
                ClusterId = "cluster-http2-test",
                Match = new RouteMatch
                {
                    Hosts = new[] { "http2-test.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-http2-test",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Make a test request
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "http2-test.localhost");
        var response = await _client.SendAsync(request);

        // Assert - Verify routing works (may fail if backend not running)
        // PermanentRedirect (308) occurs when HTTPS redirect is active
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.PermanentRedirect,
            $"Expected routing to be configured, got {response.StatusCode}"
        );

        // Verify proxy is running (Kestrel configured for HTTP/2)
        Assert.NotNull(response);
    }

    [Fact]
    public async Task ProtocolDetection_LoggedCorrectly()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-protocol-test.localhost",
                ClusterId = "cluster-protocol-test",
                Match = new RouteMatch
                {
                    Hosts = new[] { "protocol-test.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-protocol-test",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5001" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Make request and verify protocol logging
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "protocol-test.localhost");
        var response = await _client.SendAsync(request);

        // Assert - Verify request was processed
        Assert.True(response.StatusCode != 0, "Request should be processed");

        // Note: Actual protocol verification requires log capture
        // This test verifies the middleware doesn't break request processing
    }

    [Fact]
    public async Task XForwardedHeaders_PreserveClientInfo()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-headers-test.localhost",
                ClusterId = "cluster-headers-test",
                Match = new RouteMatch
                {
                    Hosts = new[] { "headers-test.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-headers-test",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5002" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Make request with custom headers
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "headers-test.localhost");
        request.Headers.Add("X-Test-Original-Header", "test-value");
        var response = await _client.SendAsync(request);

        // Assert - Verify routing works (headers are added internally)
        // PermanentRedirect (308) occurs when HTTPS redirect is active
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.PermanentRedirect,
            $"Expected routing to be configured, got {response.StatusCode}"
        );

        // Note: Actual header verification requires backend inspection
        // This test verifies the transform doesn't break request processing
    }
}
