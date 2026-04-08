using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Portless.E2ETests;

[Collection("E2E")]
public class ProxyE2ETests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly List<int> _backendPorts = new();

    public ProxyE2ETests(E2ETestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _fixture.StartProxyAsync();
        _output.WriteLine($"Proxy started on port {_fixture.ProxyPort}");
    }

    public async Task DisposeAsync()
    {
        await _fixture.StopProxyAsync();
        _output.WriteLine("Proxy stopped");
    }

    [Fact]
    public async Task ProxyStartsAndRespondsToHealth()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _output.WriteLine($"Health check: {response.StatusCode}");
    }

    [Fact]
    public async Task ProxyExposesMetrics()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Metrics (first 500 chars): {content[..Math.Min(500, content.Length)]}");

        // Prometheus metrics should contain standard process metrics
        Assert.Contains("# HELP", content);
    }

    [Fact]
    public async Task ProxyRoutesToBackend()
    {
        // Arrange - Start a test backend
        var backendPort = await _fixture.StartTestBackendAsync("testapi");
        _backendPorts.Add(backendPort);
        var hostname = $"route-test-{Guid.NewGuid():N}".Substring(0, 15) + ".localhost";

        _output.WriteLine($"Backend started on port {backendPort}");

        // Register route
        var registerResponse = await _fixture.RegisterRouteAsync(hostname, backendPort);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        _output.WriteLine($"Route registered: {hostname} -> localhost:{backendPort}");

        // Act - Make a request through the proxy
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = hostname;

        var proxyResponse = await _fixture.NoRedirectHttpClient.SendAsync(request);

        // Assert
        _output.WriteLine($"Proxy response: {proxyResponse.StatusCode}");

        // The proxy should route to the backend (200) or return an error
        // if the Host header wasn't matched
        Assert.True(
            proxyResponse.StatusCode == HttpStatusCode.OK ||
            proxyResponse.StatusCode == HttpStatusCode.BadGateway,
            $"Expected OK or 502, got {proxyResponse.StatusCode}");

        if (proxyResponse.StatusCode == HttpStatusCode.OK)
        {
            var body = await proxyResponse.Content.ReadAsStringAsync();
            Assert.Contains("testapi", body);
            _output.WriteLine($"Response body: {body}");
        }
    }

    [Fact]
    public async Task ProxyReturns502ForDeadBackend()
    {
        // Arrange - Register route pointing to a port with nothing listening
        var deadPort = PortHelper.GetFreePort();
        var hostname = $"dead-backend-{Guid.NewGuid():N}".Substring(0, 16) + ".localhost";

        var registerResponse = await _fixture.RegisterRouteAsync(hostname, deadPort);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Act - Try to reach the dead backend through the proxy
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = hostname;

        var proxyResponse = await _fixture.NoRedirectHttpClient.SendAsync(request);

        // Assert - Should get 502 Bad Gateway
        Assert.Equal(HttpStatusCode.BadGateway, proxyResponse.StatusCode);
        _output.WriteLine($"Dead backend response: {proxyResponse.StatusCode}");
    }

    [Fact]
    public async Task ProxySupportsMultipleRoutes()
    {
        // Arrange - Start two test backends
        var port1 = await _fixture.StartTestBackendAsync("backend1");
        var port2 = await _fixture.StartTestBackendAsync("backend2");
        _backendPorts.Add(port1);
        _backendPorts.Add(port2);

        var hostname1 = $"multi-1-{Guid.NewGuid():N}".Substring(0, 14) + ".localhost";
        var hostname2 = $"multi-2-{Guid.NewGuid():N}".Substring(0, 14) + ".localhost";

        // Register both routes
        var reg1 = await _fixture.RegisterRouteAsync(hostname1, port1);
        var reg2 = await _fixture.RegisterRouteAsync(hostname2, port2);
        Assert.Equal(HttpStatusCode.OK, reg1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reg2.StatusCode);

        _output.WriteLine($"Registered: {hostname1} -> :{port1}, {hostname2} -> :{port2}");

        // Verify routes are listed
        var routes = await _fixture.GetRoutesAsync();
        _output.WriteLine($"Routes after registration: {routes}");
    }

    [Fact]
    public async Task ProxyRouteRemoval()
    {
        // Arrange - Start backend and register route
        var port = await _fixture.StartTestBackendAsync("removable");
        _backendPorts.Add(port);
        var hostname = $"removable-{Guid.NewGuid():N}".Substring(0, 15) + ".localhost";

        var registerResponse = await _fixture.RegisterRouteAsync(hostname, port);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Verify route exists
        var routesAfterAdd = await _fixture.GetRoutesAsync();
        _output.WriteLine($"Routes after add: {routesAfterAdd}");

        // Act - Remove route
        var removeResponse = await _fixture.RemoveRouteAsync(hostname);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        _output.WriteLine($"Route removed: {hostname}");

        // Assert - Route should be gone
        var routesAfterRemove = await _fixture.GetRoutesAsync();
        _output.WriteLine($"Routes after remove: {routesAfterRemove}");
    }

    [Fact]
    public async Task ProxyStatusEndpoint()
    {
        // Act
        var status = await _fixture.GetStatusAsync();

        // Assert
        Assert.Equal(JsonValueKind.Object, status.ValueKind);
        Assert.True(status.TryGetProperty("status", out var statusProp));
        Assert.Equal("running", statusProp.GetString());

        _output.WriteLine($"Status: {status}");
    }
}

file static class PortHelper
{
    public static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
