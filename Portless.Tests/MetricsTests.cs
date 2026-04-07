using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Portless.Tests;

public class MetricsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MetricsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsPrometheusFormat()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("portless_active_routes", content);
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsTcpListenerMetric()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("portless_active_tcp_listeners", content);
    }
}
