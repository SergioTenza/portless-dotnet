using Portless.Core.Configuration;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class PathRoutingTests
{
    [Fact]
    public void CreateRoute_WithPath_SetsMatchPath()
    {
        var factory = new YarpConfigFactory();
        var (route, cluster) = factory.CreateRouteClusterPair("api.localhost", new[] { "http://localhost:5000" }, "/api");
        
        Assert.Equal("route-api.localhost", route.RouteId);
        Assert.Equal("cluster-api.localhost", cluster.ClusterId);
        Assert.Equal("/api", route.Match!.Path);
        Assert.Contains("api.localhost", route.Match!.Hosts!);
    }

    [Fact]
    public void CreateRoute_WithoutPath_SetsCatchAll()
    {
        var factory = new YarpConfigFactory();
        var (route, _) = factory.CreateRouteClusterPair("api.localhost", new[] { "http://localhost:5000" });
        
        Assert.Equal("/{**catch-all}", route.Match!.Path);
    }

    [Fact]
    public void CreateRoute_WithMultipleBackends_CreatesMultipleDestinations()
    {
        var factory = new YarpConfigFactory();
        var (_, cluster) = factory.CreateRouteClusterPair(
            "api.localhost",
            new[] { "http://localhost:5000", "http://localhost:5001", "http://localhost:5002" },
            "/v2");
        
        Assert.Equal(3, cluster.Destinations!.Count);
        Assert.Equal("http://localhost:5000", cluster.Destinations["backend1"].Address);
        Assert.Equal("http://localhost:5001", cluster.Destinations["backend2"].Address);
        Assert.Equal("http://localhost:5002", cluster.Destinations["backend3"].Address);
    }
}
