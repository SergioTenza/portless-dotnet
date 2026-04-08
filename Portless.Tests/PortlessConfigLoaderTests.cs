using Portless.Core.Models;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Portless.Tests;

public class PortlessConfigLoaderTests
{
    private readonly PortlessConfigLoader _loader;

    public PortlessConfigLoaderTests()
    {
        var logger = new Mock<ILogger<PortlessConfigLoader>>().Object;
        _loader = new PortlessConfigLoader(logger);
    }

    [Fact]
    public void ToRouteInfos_SingleBackend_SetsPortAndProtocol()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "api.localhost", Backends = new List<string> { "http://localhost:5000" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);

        Assert.Single(routes);
        Assert.Equal("api.localhost", routes[0].Hostname);
        Assert.Equal(5000, routes[0].Port);
        Assert.Equal("http", routes[0].BackendProtocol);
        Assert.Null(routes[0].BackendUrls); // single backend -> null
        Assert.Equal(RouteType.Http, routes[0].Type);
    }

    [Fact]
    public void ToRouteInfos_MultipleBackends_SetsBackendUrls()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "api.localhost",
                    Backends = new List<string> { "http://localhost:5000", "http://localhost:5001" },
                    LoadBalancePolicy = "RoundRobin"
                }
            }
        };

        var routes = _loader.ToRouteInfos(config);

        Assert.Single(routes);
        Assert.NotNull(routes[0].BackendUrls);
        Assert.Equal(2, routes[0].BackendUrls!.Length);
        Assert.Equal(LoadBalancingPolicy.RoundRobin, routes[0].LoadBalancingPolicy);
    }

    [Fact]
    public void ToRouteInfos_TcpRoute_SetsTypeAndListenPort()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "redis.localhost", Type = "tcp", ListenPort = 6379, Backends = new List<string> { "localhost:6379" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);

        Assert.Single(routes);
        Assert.Equal(RouteType.Tcp, routes[0].Type);
        Assert.Equal(6379, routes[0].TcpListenPort);
    }

    [Fact]
    public void ToRouteInfos_WithPath_SetsPathProperty()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "api.localhost", Path = "/v1", Backends = new List<string> { "http://localhost:5000" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);

        Assert.Single(routes);
        Assert.Equal("/v1", routes[0].Path);
    }

    [Fact]
    public void ToRouteInfos_EmptyConfig_ReturnsEmptyArray()
    {
        var config = new PortlessConfig();
        var routes = _loader.ToRouteInfos(config);
        Assert.Empty(routes);
    }

    [Fact]
    public void ToRouteInfos_SkipsRoutesWithoutHost()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "", Backends = new List<string> { "http://localhost:5000" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);
        Assert.Empty(routes);
    }
}
